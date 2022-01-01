using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;

namespace WaspPile.ShinyRat
{
    [BepInPlugin("thalber.ShinyRat", "ShinyRat", "0.0.1")]
    public class ShinyRatPlugin : BaseUnityPlugin
    {
        public ShinyRatPlugin()
        {

        }
        public void OnEnable()
        {
            if (regdone) goto skipreg;
            string subcat = "General";
            string entryname = default;
            string defval = default;
            for (int i = 0; i < ShinyConfig.profiles.Length; i++)
            {
                subcat = $"Player {i}";
                ShinyConfig.RatProfile crat = new();
                foreach (var bpt in Enum.GetValues(typeof(BP)).Cast<BP>())
                {
                    ConfigEntry<string> ccf = default;
                    entryname = bpt.ToString();
                    ShinyConfig.DefaultElmBaseNames.TryGetValue(bpt, out defval);
                    if (defval is not default(string))
                    {
                        ccf = Config.Bind(subcat, entryname, defval, $"Base sprite name for {bpt}");
                        crat.BaseElements.Add(bpt, ccf);
                    }
                }
                ShinyConfig.profiles[i] = crat;
            }
            
            regdone = true;
        skipreg:
            RefreshDebugSettings();
            ShinyHooks.Enable();
        }
        public void OnDisable()
        {
            ShinyHooks.Disable();
        }
        private bool regdone = false;

        internal const string CALLKEY = "SHINYRATSPEAKS";
        internal static void RefreshDebugSettings () => DebugString = Environment.GetEnvironmentVariable(CALLKEY);
        internal static string DebugString;
        internal static bool DebugMode => DebugString != null;
        internal static string[] DebugRules => System.Text.RegularExpressions.Regex.Split(DebugString ?? string.Empty, ", ");
    }
}
