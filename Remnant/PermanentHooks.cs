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
using WaspPile.Remnant.Martyr;

namespace WaspPile.Remnant
{
    internal static class PermanentHooks
    {
        internal static readonly Type phk_t = typeof(PermanentHooks);
        internal static List<IDetour> manualHooks = new();
        internal static void Enable()
        {
            On.MainLoopProcess.ctor += RefreshDebugSettings;
            //manualHooks.Add(new Hook(
            //    methodof<SlugBaseCharacter>(nameof(SlugBaseCharacter.CanUsePassages)), 
            //    methodof(phk_t, nameof(PassageHackby))
            //    ));
        }

        private static void RefreshDebugSettings(On.MainLoopProcess.orig_ctor orig, MainLoopProcess self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            RemnantPlugin.RefreshDebugSettings();
            LogWarning("REMNANT LOG STATUS: " + RemnantPlugin.DebugString);
            if (RemnantPlugin.DebugRules.Length > 0)
            {
                LogWarning("~~ vvvv ~~");
                foreach (var rule in RemnantPlugin.DebugRules) LogWarning(rule);
                LogWarning("~~ ^^^^ ~~");
            }
        }
        //private static bool PassageHackby(Func<SlugBaseCharacter, SaveState, bool> orig, SlugBaseCharacter self, SaveState ss)
        //    => (self is MartyrChar) ? false : orig(self, ss);

        internal static void Disable()
        {
            On.MainLoopProcess.ctor -= RefreshDebugSettings;
            foreach (var hk in manualHooks) { if (hk.IsApplied) hk.Undo(); hk.Dispose(); }
            manualHooks.Clear();
        }
    }
}
