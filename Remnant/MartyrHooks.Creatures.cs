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
            On.Lizard.ctor += makeLiz;
            On.LizardCosmetics.TailFin.DrawSprites += recolorTailFins;
            manualHooks.Add(new ILHook(typeof(LizardGraphics).GetConstructor(new[] { typeof(PhysicalObject) }), IL_makeLizGraphic));
            manualHooks.Add(new ILHook(typeof(LizardBreeds).GetMethod(nameof(LizardBreeds.BreedTemplate), allContextsStatic), IL_changeLizTemplate));
            
            foreach(var t in new[] { CRIT_CT_GOLDLIZ, CRIT_CT_GOLDCENTI })
            {
                var cgold = GetTemp(t);
                cgold.baseDamageResistance *= CRIT_GOLDEN_RESIST_MODIFIER;
            }
            var gliz = GetTemp(CRIT_CT_GOLDLIZ);
            //StaticWorld.creatureTemplates[(int)CRIT_CT_GOLDLIZ] = LizardBreeds.BreedTemplate(CRIT_CT_GOLDLIZ, gliz.ancestor, GetTemp(CreatureTemplate.Type.PinkLizard), null, null);
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
                    #region megapaste;
                    lizardBreedParams.terrainSpeeds[1] = new LizardBreedParams.SpeedMultiplier(1f, 1f, 1f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Floor, 1f, PathCost.Legality.Allowed));
                    lizardBreedParams.terrainSpeeds[2] = new LizardBreedParams.SpeedMultiplier(1f, 1f, 0.9f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Corridor, 1f, PathCost.Legality.Allowed));
                    lizardBreedParams.terrainSpeeds[3] = new LizardBreedParams.SpeedMultiplier(0.9f, 1f, 0.6f, 1f);
                    list.Add(new TileTypeResistance(AItile.Accessibility.Climb, 1f, PathCost.Legality.Allowed));
                    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToClimb, 4f, PathCost.Legality.Allowed));
                    lizardBreedParams.biteDelay = 2;
                    lizardBreedParams.biteInFront = 20f;
                    lizardBreedParams.biteRadBonus = 25f;
                    lizardBreedParams.biteHomingSpeed = 4.7f;
                    lizardBreedParams.biteChance = 1f;
                    lizardBreedParams.attemptBiteRadius = 120f;
                    lizardBreedParams.getFreeBiteChance = 1f;
                    lizardBreedParams.biteDamage = 4f;
                    lizardBreedParams.biteDamageChance = 1f;
                    lizardBreedParams.toughness = 300f;
                    lizardBreedParams.stunToughness = 3f;
                    lizardBreedParams.regainFootingCounter = 1;
                    lizardBreedParams.baseSpeed = 5f;
                    lizardBreedParams.bodyMass = 3.1f;
                    lizardBreedParams.bodySizeFac = 1.5f;
                    lizardBreedParams.floorLeverage = 6f;
                    lizardBreedParams.maxMusclePower = 9f;
                    lizardBreedParams.wiggleSpeed = 0.5f;
                    lizardBreedParams.wiggleDelay = 15;
                    lizardBreedParams.bodyStiffnes = 0.3f;
                    lizardBreedParams.swimSpeed = 1.9f;
                    lizardBreedParams.idleCounterSubtractWhenCloseToIdlePos = 10;
                    lizardBreedParams.danger = 0.8f;
                    lizardBreedParams.aggressionCurveExponent = 0.7f;
                    lizardBreedParams.headShieldAngle = 100f;
                    lizardBreedParams.canExitLounge = false;
                    lizardBreedParams.canExitLoungeWarmUp = true;
                    lizardBreedParams.findLoungeDirection = 0.5f;
                    lizardBreedParams.loungeDistance = 100f;
                    lizardBreedParams.preLoungeCrouch = 25;
                    lizardBreedParams.preLoungeCrouchMovement = -0.2f;
                    lizardBreedParams.loungeSpeed = 1.9f;
                    lizardBreedParams.loungeMaximumFrames = 20;
                    lizardBreedParams.loungePropulsionFrames = 10;
                    lizardBreedParams.loungeJumpyness = 0.5f;
                    lizardBreedParams.loungeDelay = 90;
                    lizardBreedParams.riskOfDoubleLoungeDelay = 0.1f;
                    lizardBreedParams.postLoungeStun = 20;
                    lizardBreedParams.loungeTendensy = 0.05f;
                    lizardBreedParams.perfectVisionAngle = Mathf.Lerp(1f, -1f, 0.444444448f);
                    lizardBreedParams.periferalVisionAngle = Mathf.Lerp(1f, -1f, 0.7777778f);
                    lizardBreedParams.biteDominance = 1f;
                    lizardBreedParams.limbSize = 1.75f;
                    lizardBreedParams.stepLength = 0.8f;
                    lizardBreedParams.liftFeet = 0.3f;
                    lizardBreedParams.feetDown = 0.5f;
                    lizardBreedParams.noGripSpeed = 0.25f;
                    lizardBreedParams.limbSpeed = 9f;
                    lizardBreedParams.limbQuickness = 0.8f;
                    lizardBreedParams.limbGripDelay = 1;
                    lizardBreedParams.smoothenLegMovement = true;
                    lizardBreedParams.legPairDisplacement = 0.2f;
                    lizardBreedParams.standardColor = Custom.HSL2RGB(0.13f, 1, 0.63f);
                    lizardBreedParams.walkBob = 3f;
                    lizardBreedParams.tailSegments = 19;
                    lizardBreedParams.tailStiffness = 200f;
                    lizardBreedParams.tailStiffnessDecline = 0.3f;
                    lizardBreedParams.tailLengthFactor = 1.9f;
                    lizardBreedParams.tailColorationStart = 0.3f;
                    lizardBreedParams.tailColorationExponent = 2f;
                    lizardBreedParams.headSize = 1.5f;
                    lizardBreedParams.neckStiffness = 0.2f;
                    lizardBreedParams.jawOpenAngle = 140f;
                    lizardBreedParams.jawOpenLowerJawFac = 0.6666667f;
                    lizardBreedParams.jawOpenMoveJawsApart = 23f;
                    lizardBreedParams.headGraphics = new int[5];
                    lizardBreedParams.framesBetweenLookFocusChange = 20;
                    lizardBreedParams.tamingDifficulty = 9f;
                    #endregion
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

        private static void makeLiz(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.Template.type == CreatureTemplate.Type.RedLizard)
            {
                URand.seed = abstractCreature.ID.number;
                self.effectColor = Color.yellow.RandDev(new Color(0.2f, 0.09f, 0.18f));
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

        internal static void CRIT_Disable()
        {
            On.Lizard.ctor -= makeLiz;
            On.LizardCosmetics.TailFin.DrawSprites -= recolorTailFins;   
        }
    }
}
