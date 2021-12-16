﻿//old shitty golden crits
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using WaspPile.Remnant.UAD;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static WaspPile.Remnant.RemnantUtils;
using static Mono.Cecil.Cil.OpCodes;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    public static partial class MartyrHooks
    {
        internal const float CRIT_GOLDEN_RESIST_MODIFIER = 2f;
        internal static CreatureTemplate.Type CRIT_CT_GOLDLIZ => CreatureTemplate.Type.RedLizard;
        internal static CreatureTemplate.Type CRIT_CT_GOLDCENTI => CreatureTemplate.Type.RedCentipede;
        internal class CentiGrafFields
        {
            internal CentiGrafFields(CentipedeGraphics cg)
            {
                //_ow = new WeakReference(cg);
                owner = cg;
                danglerCount = cg.owner.bodyChunks.Length - 2;
                danglers = new Dangler[danglerCount];
                for (int i = 0; i < danglerCount; i++)
                {
                    danglers[i] = new Dangler(cg, i, 6, 5f, 5f);
                }
            }
            internal readonly CentipedeGraphics owner;
            internal int bStartSprite;
            internal Dangler[] danglers;
            internal int danglerCount;
            internal Vector2 ConPos(int ind, float ts)
            {
                //TODO: better attachment points
                var res = default(Vector2);
                var c0 = owner.owner.bodyChunks[ind + 1];
                res = Vector2.Lerp(c0.lastPos, c0.pos, ts);
                return res;
            }
            internal Dangler.DanglerProps Props (int ind) 
                => new Dangler.DanglerProps() 
                { gravity = -0.02f, 
                    airFriction = 0.9f, 
                    waterGravity = 0.03f, 
                    elasticity = 0.1f, 
                    waterFriction = 0.4f, 
                    weightSymmetryTendency = 0.5f
                };
        }
        internal static readonly AttachedField<CentipedeGraphics, CentiGrafFields> centiFieldsByHash = new AttachedField<CentipedeGraphics, CentiGrafFields>();

        internal static void CRIT_Enable()
        {
            //liz
            On.Lizard.ctor += recolorLiz;
            On.LizardCosmetics.TailFin.DrawSprites += recolorTailFins;
            On.LizardCosmetics.TailFin.ctor += increaseFinSize;

            manualHooks.Add(new ILHook(ctorof<LizardGraphics>(typeof(PhysicalObject)), IL_makeLizGraphic));

            foreach (var t in new[] { CRIT_CT_GOLDLIZ, CRIT_CT_GOLDCENTI })
            {
                var cgold = GetCreatureTemplate(t);
                cgold.baseDamageResistance *= CRIT_GOLDEN_RESIST_MODIFIER;
            }
            var glizdbreed = GetCreatureTemplate(CRIT_CT_GOLDLIZ).breedParameters as LizardBreedParams;
            glizdbreed.toughness = 300f;
            glizdbreed.bodySizeFac = 1.5f;
            glizdbreed.limbSize = 1.75f;
            glizdbreed.standardColor = HSL2RGB(0.13f, 1, 0.63f);
            glizdbreed.tailSegments = 19;
            glizdbreed.headSize = 1.5f;
            glizdbreed.tamingDifficulty = 9f;
            //centi
            On.CentipedeGraphics.ctor += recolorAndRegCentiG;
            On.CentipedeGraphics.InitiateSprites += CentiG_makesprites;
            On.CentipedeGraphics.AddToContainer += CentiG_ATC;
            On.CentipedeGraphics.DrawSprites += CentiG_Draw;
            On.CentipedeGraphics.ApplyPalette += CentiG_APal;
            On.CentipedeGraphics.Update += CentiG_Update;

            manualHooks.Add(new ILHook(ctorof<Centipede>(typeof(AbstractCreature), typeof(World)), resizeCenti));
            manualHooks.Add(new Hook(methodof<Dangler>("ConPos"), methodof(typeof(MartyrHooks), nameof(EDA_conpos), allContextsStatic)));
            manualHooks.Add(new Hook(methodof<Dangler>("get_Props"), methodof(typeof(MartyrHooks), nameof(EDA_props), allContextsStatic)));
        }

        #region golden centi
        private static void CentiG_APal(On.CentipedeGraphics.orig_ApplyPalette orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (centiFieldsByHash.TryGet(self, out var cf))
            {
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    sLeaser.sprites[i + cf.bStartSprite].color = self.ShellColor;
                }
            }
        }

        private static void CentiG_Update(On.CentipedeGraphics.orig_Update orig, CentipedeGraphics self)
        {
            orig(self);
            if (centiFieldsByHash.TryGet(self, out var cf))
            {
                foreach (var d in cf.danglers) d.Update();
            }
        }

        private static void CentiG_Draw(On.CentipedeGraphics.orig_DrawSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            else if (centiFieldsByHash.TryGet(self, out var cf))
            {
                foreach (var d in cf.danglers) d.DrawSprite(cf.bStartSprite + d.danglerNum, sLeaser, rCam, timeStacker, camPos);
            }
            
        }
        private static void CentiG_ATC(On.CentipedeGraphics.orig_AddToContainer orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (CENTI_SIN_LOCK || !centiFieldsByHash.TryGet(self, out var cf)) return;
            else
            {
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[i + cf.bStartSprite]);
                }
            }
        }
        internal static bool CENTI_SIN_LOCK = false;
        private static void CentiG_makesprites(On.CentipedeGraphics.orig_InitiateSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            CENTI_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            CENTI_SIN_LOCK = false;
            if (centiFieldsByHash.TryGet(self, out var cf))
            {
                foreach (var s in sLeaser.sprites) s.RemoveFromContainer();
                cf.bStartSprite = sLeaser.sprites.Length;
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + cf.danglerCount);
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    cf.danglers[i].InitSprite(sLeaser, i + cf.bStartSprite);
                    sLeaser.sprites[i + cf.bStartSprite].shader = CRW.Shaders["TentaclePlant"];
                    sLeaser.sprites[i + cf.bStartSprite].element = Futile.atlasManager.GetElementWithName("Futile_White");
                    cf.danglers[i].Reset();
                }
                self.AddToContainer(sLeaser, rCam, null);
            }
        }

        private static Dangler.DanglerProps EDA_props(Func<Dangler, Dangler.DanglerProps> orig, Dangler self)
        {
            if (self.gModule is CentipedeGraphics cg 
                && centiFieldsByHash.TryGet(cg, out var cf))
            {
                return cf.Props(self.danglerNum);
            }
            return orig(self);
        }
        private static Vector2 EDA_conpos(Func<Dangler, float, Vector2> orig, Dangler self, float timeStacker)
        {
            if (self.gModule is CentipedeGraphics cg
                && centiFieldsByHash.TryGet(cg, out var cf))
            {
                return cf.ConPos(self.danglerNum, timeStacker);
            }
            return (orig(self, timeStacker));
        }
        private static void resizeCenti(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, xx => xx.MatchCall<PhysicalObject>("set_bodyChunks")))
            {
                Debug.LogWarning("CENTI RESIZE: FOUND INSERTION POINT");
                c.Emit(Ldarg_0);
                c.Emit(Ldarg_0);
                c.Emit(Ldfld, typeof(PhysicalObject).GetField(pbfiname(nameof(PhysicalObject.bodyChunks)), allContextsInstance));
                //c.Emit(ldfl)
                c.EmitDelegate<Action<Centipede, BodyChunk[]>>((cb, body) 
                    => { if (!cb.Red) return;
                    Array.Resize(ref body, body.Length + 5);
                    cb.bodyChunks = body;
                    Debug.Log($"CENTI RESIZE: new bodychunk count is {cb.bodyChunks.Length}");
                });
            }
            else
            {
                Debug.LogWarning("CENTI RESIZE: failed to find insertion point!");
            }
        }

        private static void recolorAndRegCentiG(On.CentipedeGraphics.orig_ctor orig, CentipedeGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (self.centipede.Red)
            {
                self.saturation = 1f;
                self.hue = 0.13f;
                centiFieldsByHash.Set(self, new CentiGrafFields(self));
            }
        }
        #endregion golden centi
        #region golden lizard

        private static void increaseFinSize(On.LizardCosmetics.TailFin.orig_ctor orig, LizardCosmetics.TailFin self, LizardGraphics lGraphics, int startSprite)
        {
            orig(self, lGraphics, startSprite);
            self.sizeRangeMin *= 1.1f;
            self.sizeRangeMax *= 1.35f;
            self.colored = true;
            self.numberOfSprites = ((!self.colored) ? self.bumps : (self.bumps * 2)) * 2;
        }
        private static void recolorLiz(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.Template.type == CRIT_CT_GOLDLIZ)
            {
                URand.seed = abstractCreature.ID.number;
                self.effectColor = Color.yellow.RandDev(new Color(0.125f, 0.09f, 0.08f));
            }
        }
        private static void recolorTailFins(On.LizardCosmetics.TailFin.orig_DrawSprites orig, LizardCosmetics.TailFin self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            float amountFadedBy = default, incrementAdded;
            if (self.lGraphics.lizard.Template.type == CreatureTemplate.Type.RedLizard)
            {
                incrementAdded = 1f / (self.bumps * 2);
                for (int i = 0; i < 2; i++)
                {
                    int num = i * ((!self.colored) ? self.bumps : (self.bumps * 2));
                    for (int j = self.startSprite; j < self.startSprite + self.bumps; j++)
                    {
                        float f = Mathf.Lerp(0.05f, self.spineLength / self.lGraphics.BodyAndTailLength, Mathf.InverseLerp(self.startSprite, self.startSprite + self.bumps - 1, j));
                        sLeaser.sprites[j + num].color = self.lGraphics.BodyColor(f);
                        if (self.colored)
                        {
                            amountFadedBy += incrementAdded;
                            sLeaser.sprites[j + self.bumps + num].color = Color.Lerp(self.lGraphics.effectColor, new Color(1f, 0f, 0f), amountFadedBy);
                        }

                    }
                }
            }
        }
        private static void IL_changeLizTemplate(ILContext il)
        {
#warning changes needed in case of separation from reds
            bool li1(Instruction xx) => xx.MatchLdcR4(1);
            var c = new ILCursor(il);
            //find the end of big break
            c.GotoNext(MoveType.Before, xx => xx.MatchLdarg(0),
                xx => xx.MatchLdarg(1),
                xx => xx.MatchLdloc(0),
                xx => xx.MatchLdloc(1),
                xx => xx.MatchLdcI4(1));
            var brl = c.Instrs[c.Index];
            c.Index = 0;
            //insert breedparams changes
            if (c.TryGotoNext(MoveType.Before,
                xx => xx.Match(Ldloc_2),
                xx => xx.MatchLdfld<LizardBreedParams>(nameof(LizardBreedParams.terrainSpeeds)),
                xx => xx.MatchLdcI4(1),
                xx => xx.MatchLdelema<LizardBreedParams.SpeedMultiplier>(),
                li1, li1, li1))
            {
                c.Emit(Ldloc_2);
                c.Emit(Ldloc_0);
                c.Emit(Ldloc_1);
                c.EmitDelegate<Action<LizardBreedParams, List<TileTypeResistance>, List<TileConnectionResistance>>>
                    ((lizardBreedParams, list, list2) =>
                {
                    lizardBreedParams.toughness = 300f;
                    lizardBreedParams.bodySizeFac = 1.5f;
                    lizardBreedParams.limbSize = 1.75f;
                    lizardBreedParams.standardColor = Custom.HSL2RGB(0.13f, 1, 0.63f);
                    lizardBreedParams.tailSegments = 19;
                    lizardBreedParams.headSize = 1.5f;
                    lizardBreedParams.tamingDifficulty = 9f;
                });
                c.Emit(Br, brl);
                Debug.LogWarning("GOLDLIZTEMPLATE: CONN PATCH INSERTED");
            }
            else
            {
                Debug.LogWarning("GOLDLIZTEMPLATE: FAILED TO FIND INSERTION POINT!");
            }

            if (c.TryGotoNext(MoveType.Before, xx => xx.MatchRet()))
            {
                c.Emit(Ldarg_2);
                c.EmitDelegate<Func<CreatureTemplate, CreatureTemplate, CreatureTemplate>>((ct, pink) =>
                {
                    if (ct.type == CRIT_CT_GOLDLIZ)
                    {
                        ct.waterPathingResistance = 2f;
                        ct.doPreBakedPathing = false;
                        ct.preBakedPathingAncestor = pink;
                        ct.visualRadius = 2300f;
                        ct.waterVision = 0.7f;
                        ct.throughSurfaceVision = 0.95f;
                    }
                    return ct;
                });
                Debug.LogWarning("GOLDLIZTEMPLATE: EXIT MOD APPLIED");
            }
            else
            {
                Debug.LogWarning("GOLDLIZTEMPLATE: FAILED TO APPLY EXIT MODIFICATION");
            }
        }
        private static void IL_makeLizGraphic(ILContext il)
        {
            var mynum = il.Body.Variables[11];
            var c = new ILCursor(il);
            c.GotoNext(ins0 => ins0.MatchLdarg(0),
                    ins1 => ins1.MatchLdarg(0),
                    ins2 => ins2.MatchLdfld(nameof(LizardGraphics), nameof(LizardGraphics.lizard)),
                    ins3 => ins3.MatchCallvirt<Creature>("get_mainBodyChunk"));
            var exitl = c.DefineLabel();
            var ex2 = c.Instrs[c.Index];
            Console.WriteLine($"exit defined");
            c.Index = 0;
            int some = default;
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out some),
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(some)
                ))
            {
                c.Emit(Ldarg_0);
                c.Emit(Ldloc_S, mynum);
                c.EmitDelegate<Func<LizardGraphics, int, int>>((self, spr) =>
                {
                    spr = self.AddCosmetic(spr, new LizardCosmetics.SpineSpikes(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.TailFin(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.LongShoulderScales(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.SpineSpikes(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.TailGeckoScales(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.JumpRings(self, spr));
                    spr = self.AddCosmetic(spr, new LizardCosmetics.ShortBodyScales(self, spr));
                    Debug.Log("GOLDLIZ cosmetics applied.");
                    return spr;
                });
                c.Emit(Stloc_S, mynum);
                c.Emit(Br, ex2);
                Console.WriteLine("GOLDLIZ: liz graphics ctor defiled successfully");
                File.WriteAllText(Path.Combine(RootFolderDirectory(), "ild.txt"), il.ToString());
            }
            else
            {
                Console.WriteLine("GOLDLIZ: FAILED TO FIND INSERTION POINT!");
            }
        }

        #endregion golden lizard

        internal static void CRIT_Disable()
        {
            //liz
            On.Lizard.ctor -= recolorLiz;
            On.LizardCosmetics.TailFin.DrawSprites -= recolorTailFins;

            //centi

            On.CentipedeGraphics.ctor -= recolorAndRegCentiG;
            On.CentipedeGraphics.InitiateSprites -= CentiG_makesprites;
            On.CentipedeGraphics.AddToContainer -= CentiG_ATC;
            On.CentipedeGraphics.DrawSprites -= CentiG_Draw;
            On.CentipedeGraphics.ApplyPalette -= CentiG_APal;
            On.CentipedeGraphics.Update -= CentiG_Update;

            On.CentipedeGraphics.ctor -= recolorAndRegCentiG;
        }
    }
}