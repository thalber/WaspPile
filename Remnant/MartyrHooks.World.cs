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
            //IL.Room.Loaded += skipKF;
            On.AbstractPhysicalObject.Realize += skipKFrealize;
        }

        private static void skipKFrealize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            if (self.type is AbstractPhysicalObject.AbstractObjectType.KarmaFlower) return;
            orig(self);
        }

        private static void skipKF(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After, xx => xx.MatchLdloc(27), xx=> xx.MatchLdcI4(1), xx => xx.MatchSub(), xx => xx.MatchSwitch(out _));
            //Instruction brp;
            var v = c.Next;//.MatchBr(out var v);
            c.GotoNext(MoveType.Before,
                xx => xx.MatchLdarg(0),
                xx => xx.MatchLdfld<Room>("game"),
                xx => xx.MatchCallOrCallvirt<RainWorldGame>("get_StoryCharacter"),
                xx => xx.MatchLdcI4(2),
                xx => xx.MatchBeq(out _),
                xx => xx.MatchLdarg(0)
                );
            c.Emit(Ldarg_0);
            c.EmitDelegate<Func<Room, bool>>(r => { var res = MartyrChar.ME.IsMe(r.game); if (RemnantPlugin.DebugMode) LogWarning("Skipping kf: " + res); return res; });
            c.Emit(Brtrue, v);
            if (RemnantPlugin.DebugMode)
            {
                LogWarning("Karma flower skip applied");
                il.dump(RootFolderDirectory(), "room.loaded.txt");
            }
        }

        internal static void WORLD_Disable()
        {

        }
    }
}
