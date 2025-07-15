using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;
using System;
using TaleWorlds.InputSystem;

namespace ImmersiveCheering
{
    internal class MCMSettings : AttributeGlobalSettings<MCMSettings>
    {
        public override string Id => "ImmersiveCheeringSettings";
        public override string DisplayName => new TextObject("Immersive Cheering").ToString();
        public override string FolderName => "ImmersiveCheering";
        public override string FormatType => "json2";

        public InputKey GetCheerKey()
        {
            InputKey key;
            try
            {
                string toUse = CheerKey;
                toUse = toUse.Length == 1 ? toUse.ToUpper() : toUse;
                key = (InputKey)Enum.Parse(typeof(InputKey), toUse);
            }
            catch (Exception) { return InputKey.V; }
            return key;
        }

        // Main settings

        [SettingPropertyBool("Enable This Modification", Order = 0, RequireRestart = false, HintText = "Toggle this modification. [Default: enabled]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public bool EnableThisModification { get; set; } = true;

        [SettingPropertyText("Cheer Key", Order = 1, RequireRestart = false, HintText = "Key to press to cheer in battle. If this value is not set correctly, it will default to V. [Default: V]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public string CheerKey { get; set; } = "V";

        [SettingPropertyInteger("Kills Count For Player Cheer", 0, 20, "0", Order = 2, RequireRestart = false, HintText = "Kills count for player cheer. [Default: 1]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public int PlayerMeterToBeAbleToCheer { get; set; } = 1;

        [SettingPropertyInteger("Leadership Threshold", 100, 500, "0", Order = 3, RequireRestart = false, HintText = "The leadership skill value at which maximum morale gain is achieved. [Default: 300]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public int LeadershipThreshold { get; set; } = 300;

        [SettingPropertyInteger("Maximum Morale Gain", 1, 20, "0", Order = 4, RequireRestart = false, HintText = "The maximum morale gain for troops when cheering. [Default: 10]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public int MaxMoraleGain { get; set; } = 10;

        [SettingPropertyInteger("Unassigned Hero Cheer Radius", 1, 50, "0", Order = 5, RequireRestart = false, HintText = "Cheering radius of unassigned hero that troops will react to. [Default: 10]")]
        [SettingPropertyGroup("Main settings", GroupOrder = 0)]
        public int UnassignedHeroCheerRadius { get; set; } = 10;

        // Technical settings

        [SettingPropertyBool("Logging for debugging", Order = 0, RequireRestart = false, HintText = "Logging for debugging. [Default: disabled]")]
        [SettingPropertyGroup("Technical settings", GroupOrder = 3)]
        public bool LoggingEnabled { get; set; } = false;
    }
}