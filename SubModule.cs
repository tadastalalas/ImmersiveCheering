using System;
using TaleWorlds.MountAndBlade;


namespace ImmersiveCheering
{
    public class SubModule : MBSubModuleBase
    {
        public static Random Random = new();

        public override void OnMissionBehaviorInitialize(Mission mission)
        {
            mission.AddMissionBehavior(new ImmersiveCheeringMissionBehavior());
        }
    }
}