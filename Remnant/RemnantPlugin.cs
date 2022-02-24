using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using WaspPile.Remnant.Martyr;

using static UnityEngine.Debug;
using static WaspPile.Remnant.RemnantConfig;
using static WaspPile.Remnant.Satellite.RemnantUtils;

namespace WaspPile.Remnant
{
    /// <summary>
    /// main BepInPlugin
    /// </summary>
    [BepInPlugin("EchoWorld.Remnant", "Remnant", "0.0.1")]
    public class RemnantPlugin : BaseUnityPlugin
    {

        public void OnEnable()
        {
            RefreshDebugSettings();
            //martyrPortrait = new UnityEngine.Texture2D(84, 84);
            //var str = MartyrChar.GetRes("MarmyrPortrait.png");
            //martyrPortrait.LoadImage(new BinaryReader(str).ReadBytes((int)str.Length));
            if (registered) goto skipReg;
            //var fel = new FAtlas("martyrsprites", UnityEngine.Texture2D.Crea //AddElement(new FAtlasElement())
            SlugBase.PlayerManager.RegisterCharacter(new MartyrChar());
            //SlugBase.PlayerManager.RegisterCharacter(new OutlawChar());
            for (int i = 0; i < abilityBinds.Length; i++)
            {
                abilityBinds[i] ??= Config.Bind("Martyr", $"Ability hotkey for P{i + 1}", UnityEngine.KeyCode.LeftAlt, $"Martyr's ability keybind for player {i + 1}");
            }
            martyrCycles ??= Config.Bind("Martyr", "Cycle limit", 10, "Number of cycles available for a run");
            martyrCure ??= Config.Bind("Martyr", "Cure effect", 3, "How many extra cycles does meeting FP give");
            noQuits ??= Config.Bind("Martyr", "No quits", true, "Exiting the game kills the run");
            registered = true;

        skipReg:
            try
            {
                if (DebugMode)
                {
                    Logger.LogWarning("REMNANT RUNNING IN DEBUG MODE! " + Environment.GetEnvironmentVariable("MARTYRDEBUG"));
                    File.WriteAllLines(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "RemnantResourceNames.txt"), Assembly.GetExecutingAssembly().GetManifestResourceNames());
                }
                if (DoTrolling)
                {
                    Logger.LogWarning("miimows style slugcat hips");
                }
                PermanentHooks.Enable();
            }
            catch { }
        }
        public void OnDisable()
        {
            PermanentHooks.Disable();
            foreach (Type t in new[] { typeof(MartyrHooks), 
                typeof(CommonHooks), 
                typeof(PermanentHooks), 
                typeof(OutlawHooks), 
                typeof(RemnantPlugin), 
                //typeof(Satellite.ArenaIcons) 
            })
            {
                t.CleanUpStatic();
            }
        }
        //public void Update()
        //{
        //    if (atlasesRegistered) return;
        //    if (Futile.atlasManager is not null)
        //    {
        //        var nat = Futile.atlasManager.LoadAtlasFromTexture(martyrFaceName, martyrPortrait);
        //        atlasesRegistered |= nat is not null;
        //    }
        //}
        //internal bool atlasesRegistered = false;
        //internal UnityEngine.Texture2D martyrPortrait;
        internal const string martyrFaceName = "marmyrPortrait";

        //some old in-dev names
        internal static bool DoTrolling => File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "gatobabosa.txt"));
        internal const string CALLKEY = "MARTYRDEBUG";
        internal static void RefreshDebugSettings() => DebugString = Environment.GetEnvironmentVariable(CALLKEY);
        internal static string DebugString;
        internal static bool DebugMode => DebugString != null;
        internal static string[] DebugRules => DebugMode? System.Text.RegularExpressions.Regex.Split(DebugString, ", ") : new string[0];
        private bool registered = false;
    }

}
