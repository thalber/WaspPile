using BepInEx;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;

using static WaspPile.ShinyRat.Satellite.RatUtils;

using UD = UnityEngine.Debug;

namespace WaspPile.ShinyRat
{
    [BepInPlugin("thalber.ShinyRat", "ShinyRat", "0.0.1")]
    public class ShinyRatPlugin : BaseUnityPlugin
    {
        public ShinyRatPlugin()
        {
            __me = new(this);
        }
        // AU
        // ------------------------------------------------
        public string updateURL = "http://beestuff.pythonanywhere.com/audb/api/mods/5/3";
        public int version = 2;
        public string keyE = "AQAB";
        public string keyN = "uwptqosDNjimqNbRwCtJIKBXFsvYZN+b7yl668ggY46j+2Zlm/+L9TpypF6Bhu85CKnkY7ffFCQixTSzumdXrz1WVD0PTvoKDAp33U/loKHoAe/rs3HwdaOAdpug//rIGDmtwx56DC05NiLYKVRf4pS3yM1xN39Rr2at/RmAxdamKLUnoJtHRwx2eGsoKq5dmPZ7BKTmF/49N6eFUvUXEF9evPRfAdPH9bYAMNx0QS3G6SYC0IQj5zWm4FnY1C57lmvZxQgqEZDCVgadphJAjsdVAk+ZruD0O8X/dqXiIBSdEjZsvs4VDsjEF8ekHoon2UZnMEd6XocIK4CBqJ9HCMGaGZusnwhtVsGyMur1Go4w0CXDH3L5mKhcEm/V7Ik2RV5/Z2Kz8555fO7/9UiDC9vh5kgk2Mc04iJa9rcWSMfrwzrnvzHZzKnMxpmc4XoSqiExVEVJszNMKqgPiQGprkfqCgyK4+vbeBSXx3Ftalncv9acU95qxrnbrTqnyPWAYw3BKxtsY4fYrXjsR98VclsZUFuB/COPTI/afbecDHy2SmxI05ZlKIIFE/+yKJrY0T/5cT/d8JEzHvTNLOtPvC5Ls1nFsBqWwKcLHQa9xSYSrWk8aetdkWrVy6LQOq5dTSD4/53Tu0ZFIvlmPpBXrgX8KJN5LqNMmml5ab/W7wE=";
        // ------------------------------------------------

        public void OnEnable()
        {
            RefreshDebugSettings();
            if (regdone) goto skipreg;
            string subcategory = "General";
            string entryname = default;
            string defval = default;
            for (int i = 0; i < ShinyConfig.profiles.Length; i++)
            {
                subcategory = $"Player {i + 1}";
                ShinyConfig.RatProfile currentRat = new();
                foreach (var bpt in Enum.GetValues(typeof(BP)).Cast<BP>())
                {
                    entryname = bpt.ToString();
                    ShinyConfig.DefaultElmBaseNames.TryGetValue(bpt, out defval);
                    if (defval is not default(string))
                    {
                        ShinyConfig.SpriteGroupInfo csg = new()
                        {
                            baseElm = Config.Bind(subcategory, entryname, defval, $"Base sprite name for {bpt}"),
                            scaleX = Config.Bind(subcategory, entryname + "_scaleX", 1f, $"Sprite scale X factor for {bpt}"),
                            scaleY = Config.Bind(subcategory, entryname + "_scaleY", 1f, $"Sprite scale Y factor for {bpt}")
                        };

                        currentRat.BodyPartSettings.Add(bpt, csg);
                    }
                }
                //crat.bodyCol = Config.Bind(subcat, "Body Color", Color.white, "Body color");
                //crat.faceCol = Config.Bind(subcat, "Face Color", Color.white, "Face color");
                string[] channels = new[] { "R", "G", "B" };
                for (int j = 0; j < 3; j++)
                {
                    var chan = channels[j];
                    currentRat.FaceCol[j] = Config.Bind(subcategory, "Face Color " + chan, 125f, $"Face color {chan} channel; should be between 0 and 255");
                    currentRat.BodyCol[j] = Config.Bind(subcategory, "Body Color " + chan, 255f, $"Body color {chan} channel; should be between 0 and 255");
                    currentRat.TTHandCol[j] = Config.Bind(subcategory, "OnTopOfTerrainHand color " + chan, 255f, $"OnTopOfTerrain hands {chan} channel; should be between 0 and 255");
                }
                currentRat.enabled = Config.Bind(subcategory, "Profile enabled", true, "Turn off if you want sprites to remain unchanged for " + subcategory);
                currentRat.yieldToCT = Config.Bind(subcategory, "Cooperate with CustomTail", true, "If CustomTail mod is present, ShinyRat won't modify tail texture and color for this profile.");
                ShinyConfig.profiles[i] = currentRat;
            }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                CustomTailsExist |= asm.FullName.Contains("CustomTail");
            }
            if (DebugMode) Debug.LogWarning("Custom tail found: " + CustomTailsExist);
            regdone = true;
        skipreg:
            ShinyHooks.Enable();
        }
        public void OnDisable()
        {
            ShinyHooks.Disable();
            foreach (var t in new[] { typeof(ShinyConfig), typeof(ShinyRatPlugin) }) t.CleanUp();
        }
        private bool regdone = false;
        
        internal Texture2D ratTex;
        internal bool atlasDone = false;
        public void savecfg() => Config.Save();
        public void Update()
        {
            if (atlasDone) return;
            if (Futile.atlasManager is not null)
            {
                ratTex = new(125, 125);
                using var str = Assembly.GetExecutingAssembly().GetManifestResourceStream("WaspPile.ShinyRat.assets.plat.png"); BinaryReader br = new(str);
                ratTex.LoadImage(br.ReadBytes((int)str.Length));
                Futile.atlasManager.LoadAtlasFromTexture("ShinyRat", ratTex);
                atlasDone = true;
                if (DebugMode) Logger.LogWarning("rat texture registered");
                //if (DebugMode) Futile.atlasManager.LogAllElementNames();
            }
        }

        private static WeakReference __me;
        internal static ShinyRatPlugin ME => __me?.Target as ShinyRatPlugin;

        internal static bool CustomTailsExist;
        internal const string CALLKEY = "SHINYRATSPEAKS";
        internal static void RefreshDebugSettings () => DebugString = Environment.GetEnvironmentVariable(CALLKEY);
        internal static string DebugString;
        internal static bool DebugMode => DebugString != null;
        internal static string[] DebugRules => System.Text.RegularExpressions.Regex.Split(DebugString ?? string.Empty, ", ");
        public static object LoadOI()
        {
            try
            {
                var res = new ShinyOI(ME);
                if (DebugMode)
                {
                    Console.WriteLine(res);
                    Console.WriteLine(ME);
                }
                return res;
            }
            catch (TypeLoadException)
            {
                UD.LogWarning("SHINYRAT: CM not present.");
                return null;
            }
        }
    }
}
