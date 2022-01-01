using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;

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
                subcat = $"Player {i + 1}";
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
                //crat.bodyCol = Config.Bind(subcat, "Body Color", Color.white, "Body color");
                //crat.faceCol = Config.Bind(subcat, "Face Color", Color.white, "Face color");
                string[] channels = new[] { "R", "G", "B" };
                for (int j = 0; j < 3; j++)
                {
                    crat.FaceColorElms[j] = Config.Bind(subcat, "Face Color " + channels[j], 255f, $"Face color {channels[j]} channel; should be between 0 and 255");
                    crat.BodyColorElms[j] = Config.Bind(subcat, "Body Color " + channels[j], 255f, $"Body color {channels[j]} channel; should be between 0 and 255");
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
