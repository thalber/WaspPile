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

        internal static void CRIT_Enable()
        {
            On.Lizard.ctor += makeLiz;
#warning finish unhooking
            On.LizardCosmetics.TailFin.DrawSprites += recolorTailFins;
            //IL.LizardGraphics.ctor += IL_makeLizGraphic;
            //manualHooks.Add(new ILHook(mhk_t.GetMethod(nameof())));
            var lizgc = typeof(LizardGraphics).GetMethod(".ctor", allContextsCtor);
            Console.WriteLine($"{lizgc}");
            manualHooks.Add(new ILHook(lizgc, IL_makeLizGraphic));
            foreach(var t in new[] { CreatureTemplate.Type.RedLizard, CreatureTemplate.Type.RedCentipede })
            {
                var cgold = GetTemp(CreatureTemplate.Type.RedLizard);
                cgold.baseDamageResistance *= CRIT_GOLDEN_RESIST_MODIFIER;
                
            }
        }

        private static void IL_makeLizGraphic(ILContext il)
        {
#warning add other cosmetics
            //Debug.Log("lizgraphctor il:\n" + il);
            var mynum = il.Body.Variables[11];
            var c = new ILCursor(il);
            c.GotoNext(ins0 => ins0.MatchLdarg(0),
                    ins1 => ins1.MatchLdarg(0),
                    ins2 => ins2.MatchLdfld(nameof(LizardGraphics), nameof(LizardGraphics.lizard)),
                    ins3 => ins3.MatchCallvirt<Creature>("get_mainBodyChunk"));
            var exit = c.DefineLabel();
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
                //il.DefineLabel(cursors[0].Instrs);
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
                    return spr;
                });
                c.Emit(Stloc_S, mynum);
#warning make sure label pick
                c.Emit(Br, exit);
                Console.WriteLine("MARTYRLIZ: liz graphics ctor defiled successfully");
                File.WriteAllText(Path.Combine(RootFolderDirectory(), "ild.txt"), il.ToString());
            }
            else
            {
                Console.WriteLine("MARTYRLIZ: FAILED TO FIND INSERTION POINT!");
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
            float amountFadedBy = default, incrementAdded = default;
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
                amountFadedBy = 0f;
            }
        }

        internal static void CRIT_Disable()
        {
            On.Lizard.ctor -= makeLiz;
            On.LizardCosmetics.TailFin.DrawSprites -= recolorTailFins;
            IL.LizardGraphics.ctor -= IL_makeLizGraphic;
            try
            {
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StaticWorld).TypeHandle);
            }
            catch (Exception e) { Debug.Log("Could not call staticworld.cctor! " + e.Message); }
            
        }
    }
}
