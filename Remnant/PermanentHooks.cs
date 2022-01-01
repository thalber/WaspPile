using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Reflection;
using WaspPile.Remnant.UAD;
using SlugBase;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    internal static class PermanentHooks
    {
        internal static List<IDetour> manualHooks = new();
        internal static void Enable()
        {
            On.MainLoopProcess.ctor += RefreshDebugSettings;
        }

        private static void RefreshDebugSettings(On.MainLoopProcess.orig_ctor orig, MainLoopProcess self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            RemnantPlugin.RefreshDebugSettings();
            LogWarning("REMNANT LOG STATUS : " + RemnantPlugin.DebugString);
        }

        internal static void Disable()
        {
            On.MainLoopProcess.ctor -= RefreshDebugSettings;
        }
    }
}
