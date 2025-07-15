using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Library;

namespace ImmersiveCheering
{
    internal class Utilities
    {
        private readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        private void LogMessage(string message)
        {
            if (settings.LoggingEnabled)
            {
                InformationManager.DisplayMessage(new InformationMessage(message, Colors.Yellow));
            }
        }
    }
}