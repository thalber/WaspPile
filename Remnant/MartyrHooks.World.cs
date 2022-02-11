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
    public static partial class MartyrHooks
    {
        internal static void WORLD_Enable()
        {
            manualHooks.Add(new ILHook(methodof<Oracle>("SetUpSwarmers"), KillMoon));
            On.AbstractPhysicalObject.Realize += skipKFrealize;
            On.GhostWorldPresence.SpawnGhost += skipEchoPriming;
            On.VoidSea.PlayerGhosts.AddGhost += skipPlayerGhosts;
            IL.RainWorldGame.ExitToVoidSeaSlideShow += removeEnding;
        }

        private static void removeEnding(ILContext il)
        {
            ILCursor c = new(il);
            c.Emit(Ldarg_0);
            c.EmitDelegate<Action<RainWorldGame>>(rwg =>
            {
                if (rwg.TryGetSave<MartyrChar.MartyrSave>(out var ms))
                {
                    ms.imDone = true;
                }
            });
            c.GotoNext(MoveType.Before,
                xx => xx.MatchCallOrCallvirt<ProcessManager>("RequestMainProcessSwitch"),
                xx => xx.MatchRet());
            c.Prev.Operand = (int)ProcessManager.ProcessID.Credits;
        }

        private static void skipPlayerGhosts(On.VoidSea.PlayerGhosts.orig_AddGhost orig, VoidSea.PlayerGhosts self)
        {
            
        }
        private static bool skipEchoPriming(On.GhostWorldPresence.orig_SpawnGhost orig, GhostWorldPresence.GhostID ghostID, int karma, int karmaCap, int ghostPreviouslyEncountered, bool playingAsRed)
        {
            return orig(ghostID, karma, karmaCap, ghostPreviouslyEncountered, true);
        }
        private static void skipKFrealize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (self.type is AbstractPhysicalObject.AbstractObjectType.KarmaFlower) return;
            orig(self);
        }

        internal static void WORLD_Disable()
        {
            On.AbstractPhysicalObject.Realize -= skipKFrealize;
            On.GhostWorldPresence.SpawnGhost -= skipEchoPriming;
            On.VoidSea.PlayerGhosts.AddGhost -= skipPlayerGhosts;
            IL.RainWorldGame.ExitToVoidSeaSlideShow -= removeEnding;
        }
    }
}
