using System.Collections.Generic;
using System.Linq;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace ImmersiveCheering
{
    internal class ImmersiveCheeringMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        private readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        private readonly List<(List<Agent> Agents, float Timer, Agent Leader)> cheeringGroups = new();

        private readonly HashSet<Agent> agentsThatStartedCheering = new HashSet<Agent>();

        private readonly ActionIndexCache[] cheerActions = new ActionIndexCache[]
        {
            ActionIndexCache.Create("act_cheer_1"),
            ActionIndexCache.Create("act_cheer_2"),
            ActionIndexCache.Create("act_cheer_3"),
            ActionIndexCache.Create("act_cheer_4")
        };

        private int currentPlayerCheerMeter = 0;

        public override void OnMissionTick(float dt)
        {
            if (!IsMissionEligibleForCheering())
                return;

            HandlePlayerCheering(dt);
            UpdateCheeringGroups(dt);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);

            if (!IsMissionEligibleForCheering() || affectedAgent == null || affectorAgent == null || affectedAgent == affectorAgent)
                return;

            IncrementPlayerCheerMeter(affectorAgent, 1);
        }

        private void HandlePlayerCheering(float dt)
        {
            Agent controlledAgent = GetControlledAgent();
            if (controlledAgent != null && Input.IsKeyPressed(settings.GetCheerKey()) && (currentPlayerCheerMeter >= settings.PlayerMeterToBeAbleToCheer))
            {
                HandleAgentCheering(controlledAgent);
                ResetPlayerCheerMeter();
            }
        }

        private Agent GetControlledAgent()
        {
            // Prefer Agent.Main, but if not valid, try to find the currently controlled agent
            if (Agent.Main != null && Agent.Main.IsPlayerControlled)
                return Agent.Main;

            // Fallback: find any agent that is player-controlled
            return Mission.Current?.Agents.FirstOrDefault(a => a.IsPlayerControlled);
        }

        private void HandleAgentCheering(Agent agent)
        {
            if (!IsAgentEligibleToCheer(agent) || agent.Character == null)
                return;

            // Create a new group for the agent who started cheering
            var newGroup = new List<Agent> { agent };
            cheeringGroups.Add((newGroup, 0f, agent));

            // Calculate cheer percentage based on leadership skill
            int leadershipSkill = agent.Character.GetSkillValue(DefaultSkills.Leadership);
            float leadershipThreshold = settings.LeadershipThreshold;
            float cheerPercentage = MathF.Min(leadershipSkill / leadershipThreshold, 1f); // Clamp to 100%

            // If the agent is the general, schedule cheering for their troops after 2 seconds
            if (agent.Team != null && agent.Team.GeneralAgent == agent)
            {
                var generalTroopGroup = agent.Team.ActiveAgents
                    .Where(teamAgent => teamAgent != agent && IsAgentEligibleToCheer(teamAgent))
                    .ToList();

                AddAgentsToCheeringGroup(generalTroopGroup, cheerPercentage, -2f, agent);
            }
            // If the agent is a captain, cheer for their formation
            else if (agent.Formation != null && agent == agent.Formation.Captain)
            {
                var captainTroopGroup = agent.Formation.Team.ActiveAgents
                    .Where(formationAgent => formationAgent.Formation != null && formationAgent.Formation.Captain == agent.Formation.Captain && formationAgent != agent && IsAgentEligibleToCheer(formationAgent))
                    .ToList();

                AddAgentsToCheeringGroup(captainTroopGroup, cheerPercentage, -2f, agent);
            }
            // If the agent is an unassigned hero, cheer in radius
            else if (IsUnassignedHero(agent))
            {
                var nearbyAgents = Mission.Current.Agents
                    .Where(a => a != agent && IsAgentEligibleToCheer(a) && a.Position.Distance(agent.Position) <= settings.UnassignedHeroCheerRadius)
                    .ToList();

                AddAgentsToCheeringGroup(nearbyAgents, cheerPercentage, -2f, agent);
            }
        }

        private void AddAgentsToCheeringGroup(List<Agent> agents, float cheerPercentage, float initialDelay, Agent leader)
        {
            // Calculate the number of agents that will cheer
            int agentsToCheerCount = (int)(agents.Count * cheerPercentage);

            // Randomly select agents to cheer
            var selectedAgents = agents.OrderBy(_ => MBRandom.RandomFloat).Take(agentsToCheerCount).ToList();

            // Add each selected agent to the cheering group with a random delay
            foreach (var agent in selectedAgents)
            {
                float randomDelay = MBRandom.RandomFloatRanged(0.1f, 1.0f); // Random delay between 0.1 and 1 second
                cheeringGroups.Add((new List<Agent> { agent }, initialDelay + randomDelay, leader));
            }
        }

        private void UpdateCheeringGroups(float dt)
        {
            var groupsToRemove = new List<(List<Agent>, float, Agent)>();

            for (int i = 0; i < cheeringGroups.Count; i++)
            {
                var (agents, timer, leader) = cheeringGroups[i];
                timer += dt;

                if (timer < 2f && timer >= 0f) // Start cheering if timer reaches 0
                {
                    foreach (var agent in agents)
                    {
                        if (!agentsThatStartedCheering.Contains(agent)) // Ensure agent cheers only once
                        {
                            MakeAgentCheer(agent, leader);
                            agentsThatStartedCheering.Add(agent); // Mark agent as cheered
                        }
                    }
                }

                if (timer >= 2f) // Stop cheering after 2 seconds
                {
                    foreach (var agent in agents)
                    {
                        StopAgentCheer(agent);
                        agentsThatStartedCheering.Remove(agent); // Remove agent from the tracking set
                    }
                    groupsToRemove.Add(cheeringGroups[i]);
                }
                else
                {
                    cheeringGroups[i] = (agents, timer, leader); // Update the timer for the group
                }
            }

            // Remove groups that have finished cheering
            foreach (var group in groupsToRemove)
            {
                cheeringGroups.Remove(group);
            }
        }

        private bool IsUnassignedHero(Agent agent) => agent.Character != null && agent.IsHero && agent.Team.GeneralAgent != agent && agent.Formation?.Captain != agent;

        private bool IsAgentEligibleToCheer(Agent agent) => agent != null && agent.IsActive() && agent.IsHuman;

        private void MakeAgentCheer(Agent agent, Agent leader)
        {
            agent.SetActionChannel(1, cheerActions[MBRandom.RandomInt(cheerActions.Length)], false, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
            agent.MakeVoice(SkinVoiceManager.VoiceType.Victory, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
            BoostMorale(agent, leader);
        }

        private void BoostMorale(Agent agent, Agent leader)
        {
            // Retrieve the hero's leadership skill
            int leadershipSkill = leader.Character?.GetSkillValue(DefaultSkills.Leadership) ?? 0;

            // Retrieve settings for morale calculation
            float leadershipThreshold = settings.LeadershipThreshold;
            float maxMoraleGain = settings.MaxMoraleGain;

            // Calculate morale gain dynamically based on leadership skill
            float moraleGain = maxMoraleGain * (leadershipSkill / leadershipThreshold);
            moraleGain = MathF.Min(moraleGain, maxMoraleGain); // Cap morale gain at the maximum value

            // Round morale gain to the nearest integer
            int roundedMoraleGain = (int)MathF.Round(moraleGain);

            // Safely adjust the agent's morale
            agent.SetMorale(agent.GetMorale() + roundedMoraleGain);
        }

        private void StopAgentCheer(Agent agent)
        {
            if (agent != null && agent.IsActive())
                agent.SetActionChannel(1, ActionIndexCache.act_none, true, 0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
        }

        private void ResetPlayerCheerMeter()
        {
            currentPlayerCheerMeter = 0;
        }

        private void IncrementPlayerCheerMeter(Agent affectorAgent, int increment)
        {
            if (currentPlayerCheerMeter >= settings.PlayerMeterToBeAbleToCheer)
                return;

            if (affectorAgent == Agent.Main)
                currentPlayerCheerMeter += increment;
        }

        private bool IsMissionEligibleForCheering()
        {
            return settings.EnableThisModification && Mission.Current != null && (Mission.Current.IsFieldBattle || Mission.Current.IsSallyOutBattle || Mission.Current.IsSiegeBattle);
        }
    }
}