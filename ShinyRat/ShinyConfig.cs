using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;
using static WaspPile.ShinyRat.Satellite.RatUtils;

namespace WaspPile.ShinyRat
{
    internal static class ShinyConfig
    {
        internal static RatProfile GetVisProfile(this Player p)
        {
            var pnum = p?.room?.game?.Players.IndexOf(p?.abstractCreature) ?? 0;
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
        
        internal class RatProfile
        {
            internal void ReadProfile(string[] text)
            {
                foreach (string l in text)
                {
                    var split = Regex.Split(l, " : ");
                    switch (split.Length)
                    {
                        case 2:
                            if (split[0].TryParseEnum<BP>(out var bpin))
                            {
                                BaseElements[bpin].Value = split[1];
                            }
                            else if (split[0] == "enabled" && bool.TryParse(split[1], out var r))
                            {
                                enabled.Value = r;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            internal readonly Dictionary<BP, ConfigEntry<string>> BaseElements = new();
            internal readonly ConfigEntry<float>[] FaceCol = new ConfigEntry<float>[3];
            internal readonly ConfigEntry<float>[] BodyCol = new ConfigEntry<float>[3];
            internal readonly ConfigEntry<float>[] TTHandCol = new ConfigEntry<float>[3];
            internal Color faceCol
            {
                get => (new Color(FaceCol[0].Value, FaceCol[1].Value, FaceCol[2].Value) / 255f).Clamped();
                set {
                    for (int i = 0; i < 3; i++)
                    {
                        FaceCol[i].Value = value[i] * 255f;
                    }
                }
            }
            internal Color bodyCol
            {
                get => (new Color(BodyCol[0].Value, BodyCol[1].Value, BodyCol[2].Value) / 255f).Clamped();
                set
                {
                    for (int i = 0; i < 3; i++)
                    {
                        BodyCol[i].Value = value[i] * 255f;
                    }
                }
            }
            internal Color TTHCol
            {
                get => (new Color(TTHandCol[0].Value, TTHandCol[1].Value, TTHandCol[2].Value) / 255f).Clamped();
                set
                {
                    for (int i = 0; i < 3; i++)
                    {
                        TTHandCol[i].Value = value[i] * 255f;
                    }
                }
            }
            internal ConfigEntry<bool> enabled;
            internal ConfigEntry<bool> yieldToCT;
        }
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
