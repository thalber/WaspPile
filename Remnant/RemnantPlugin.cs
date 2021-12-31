﻿using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

using static WaspPile.Remnant.RemnantConfig;

namespace WaspPile.Remnant
{

    [BepInPlugin("EchoWorld.Remnant", "Remnant", "0.0.1")]
    public class RemnantPlugin : BaseUnityPlugin
    {

        public void OnEnable()
        {
            if (registered) goto skipReg;

            SlugBase.PlayerManager.RegisterCharacter(new MartyrChar());
            //SlugBase.PlayerManager.RegisterCharacter(new OutlawChar());
            for (int i = 0; i < abilityBinds.Length; i++)
            {
                if (abilityBinds[i] == null) abilityBinds[i] = Config.Bind("Martyr", $"Ability hotkey for P{i + 1}", UnityEngine.KeyCode.LeftAlt, $"Martyr's ability keybind for player {i + 1}");
            }
            if (martyrCycles == null) martyrCycles = Config.Bind("Martyr", "Cycle limit", 10, "Number of cycles available for a run");
            if (noQuits == null) noQuits = Config.Bind("Martyr", "No quits", true, "Exiting the game kills the run");
            registered = true;

        skipReg:
            try
            {
                if (DebugMode)
                {
                    Logger.LogWarning("REMNANT RUNNING IN DEBUG MODE! " + Environment.GetEnvironmentVariable("MARTYRDEBUG"));
                }
                if (DoTrolling)
                {
                    Logger.LogWarning("miimows style slugcat hips");
                }
                //var resnames = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                //File.WriteAllLines(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "resnames.txt"), resnames);
            }
            catch { }

        }
        internal static bool DoTrolling => File.Exists(Path.Combine(RWCustom.Custom.RootFolderDirectory(), "gatobabosa.txt"));
        internal static bool DebugMode => DebugRules != null;
        internal static string DebugRules => Environment.GetEnvironmentVariable("MARTYRDEBUG");
        internal static string[] DebugRulesSplit => System.Text.RegularExpressions.Regex.Split(DebugRules ?? string.Empty, ", ");
        private bool registered = false;
    }

}
