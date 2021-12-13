//old shitty golden crits
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

        internal static void CRIT_Enable()
        {
            //liz
            On.Lizard.ctor += recolorLiz;
            On.LizardCosmetics.TailFin.DrawSprites += recolorTailFins;
            On.LizardCosmetics.TailFin.ctor += increaseFinSize;

            manualHooks.Add(new ILHook(rsh_getctor<LizardGraphics>(typeof(PhysicalObject)), IL_makeLizGraphic));

            foreach (var t in new[] { CRIT_CT_GOLDLIZ, CRIT_CT_GOLDCENTI })
            {
                var cgold = GetCreatureTemplate(t);
                cgold.baseDamageResistance *= CRIT_GOLDEN_RESIST_MODIFIER;
            }
            var goldbreed = GetCreatureTemplate(CRIT_CT_GOLDLIZ).breedParameters as LizardBreedParams;
            goldbreed.toughness = 300f;
            goldbreed.bodySizeFac = 1.5f;
            goldbreed.limbSize = 1.75f;
            goldbreed.standardColor = HSL2RGB(0.13f, 1, 0.63f);
            goldbreed.tailSegments = 19;
            goldbreed.headSize = 1.5f;
            goldbreed.tamingDifficulty = 9f;
            //centi
            On.CentipedeGraphics.ctor += recolorCentis;
            manualHooks.Add(new ILHook(rsh_getctor<Centipede>(), resizeCenti));
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

        private static void recolorCentis(On.CentipedeGraphics.orig_ctor orig, CentipedeGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (self.centipede.Red)
            {
                self.saturation = 1f;
                self.hue = 0.13f;
            }
        }

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
                        //TODO: check practical effects
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

            On.CentipedeGraphics.ctor -= recolorCentis;
        }
    }
}
