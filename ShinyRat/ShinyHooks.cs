using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

using static WaspPile.ShinyRat.Satellite.RatUtils;
using static WaspPile.ShinyRat.ShinyConfig;
using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;


using SG = On.PlayerGraphics;

namespace WaspPile.ShinyRat
{
    public static partial class ShinyHooks
    {
        internal static readonly List<IDetour> manualHooks = new();
        public static void Enable()
        {
            //idr
            SG.InitiateSprites += SG_InitiateSprites;
            SG.AddToContainer += SG_AddToContainer;
            SG.DrawSprites += SG_DrawSprites;
            SG.ApplyPalette += SG_ApplyPalette;
            On.MainLoopProcess.ctor += WriteAllElms;
        }

        private static void WriteAllElms(On.MainLoopProcess.orig_ctor orig, MainLoopProcess self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            ShinyRatPlugin.RefreshDebugSettings();
            if (!ShinyRatPlugin.DebugMode) return;
            LogWarning("[ BEGIN ELM REPLACEMENT DUMP ]");
            for (int i = 0; i < profiles.Length; i++)
            {
                LogWarning($"Profile {i}:");
                foreach (var ovr in profiles[i].BaseElements)
                {
                    LogWarning($"{ovr.Key}, {ovr.Value.Value}");
                }
                LogWarning("_ _ _");
            }
            LogWarning("[ END ELM REPLACEMENT DUMP ]");
        }

        #region idrawable
        private static void SG_ApplyPalette(SG.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette pal)
        {
            orig(self, sLeaser, rCam, pal);
        }

        private static void SG_DrawSprites(SG.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float ts, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, ts, camPos);
            var cprof = self.player.GetVisProfile();
            if (cprof == null) return;
            try
            {
                var sprites = sLeaser.sprites;
                foreach (var kvp in BpToIndex)
                {
                    BP cbp = kvp.Key;
                    foreach (int i in kvp.Value) sprites[i].color = (cbp == BP.face) ? cprof.faceCol : cprof.bodyCol;
                    cprof.BaseElements.TryGetValue(cbp, out var en);
                    if (en == default) continue;
                    string stateInd = string.Empty;
                    foreach (int i in kvp.Value)
                    {
                        var csprite = sprites[i];
                        var groupName = en.Value;
                        string pattern = string.Empty;//"[^0-9AB]";
                        switch (cbp)
                        {
                            //todo: regex into objects?
                            case BP.face:
                                pattern = "[^0-9AB]";
                                break;
                            default:
                                pattern = "[^0-9]";
                                break;
                        }
                        stateInd = Regex.Replace(csprite.element.name, pattern, string.Empty);
                        var fullElmName = groupName + stateInd;
                        if (Futile.atlasManager.DoesContainElementWithName(fullElmName))
                        {
                            csprite.element = Futile.atlasManager.GetElementWithName(fullElmName);
                        }
                        
                        //if (ShinyRatPlugin.DebugRules.Contains("writeall"))
                        //{
                        //    //LogBunch(__arglist(fullElmName));
                        //    Log(fullElmName);
                        //}
                    }
                }
            }
            catch (Exception e)
            {
                LogError("ERROR ON SLUGGRAPHICS.DRAWSPRITES: " + e);
            }
            
        }

        private static void SG_AddToContainer(SG.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer ncon)
        {
            orig(self, sLeaser, rCam, ncon);
        }

        private static void SG_InitiateSprites(SG.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
        }
        #endregion idrawable

        public static void Disable()
        {
            //idr
            SG.InitiateSprites -= SG_InitiateSprites;
            SG.AddToContainer -= SG_AddToContainer;
            SG.DrawSprites -= SG_DrawSprites;
            SG.ApplyPalette -= SG_ApplyPalette;
            On.MainLoopProcess.ctor -= WriteAllElms;
        }
    }
}
