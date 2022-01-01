using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

namespace WaspPile.ShinyRat
{
    internal static class ShinyConfig
    {
        internal class RatProfile
        {
            internal readonly Dictionary<BP, ConfigEntry<string>> BaseElements = new();
        }
        internal static RatProfile GetVisProfile(this Player p)
        {
            var pnum = p.room.game.Players.IndexOf(p.abstractCreature);
            return profiles[IntClamp(pnum, 0, profiles.Length - 1)];
        }
        internal static RatProfile[] profiles = new RatProfile[4];
        internal static readonly Dictionary<BP, string> DefaultElmBaseNames = new()
        {
            { BP.head, "HeadA" },
            { BP.body, "BodyA" },
            { BP.face, "FaceA" },
            { BP.hips, "HipsA" },
            { BP.arm, "PlayerArm" },
            { BP.legs, "LegsA" },
            { BP.tail, "Futile_White" }
        };
        internal static readonly Dictionary<BP, int[]> BpToIndex = new()
        {
            { BP.body, new[] { 0 } },
            { BP.hips, new[] { 1 } },
            { BP.tail, new[] { 2 } },
            { BP.head, new[] { 3 } },
            { BP.legs, new[] { 4 } },
            { BP.arm, new[] { 5, 6 } },
            { BP.hand, new[] { 7, 8 } },
            { BP.face, new[] { 9 } },
        };
    }

    internal enum BP
    {
        body,
        hips,
        tail,
        head,
        legs,
        arm, 
        hand,
        face,
    }
}
