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
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    public static partial class MartyrHooks
    {
        internal const float CRIT_GOLDEN_RESIST_MODIFIER = 25f;
        internal static CreatureTemplate.Type CRIT_CT_GOLDLIZ => CreatureTemplate.Type.RedLizard;
        internal static CreatureTemplate.Type CRIT_CT_GOLDCENTI => CreatureTemplate.Type.RedCentipede;
        internal static CreatureTemplate.Type CRIT_CT_GOLDSPITTER => CreatureTemplate.Type.SpitterSpider;
        internal static bool IsGolden(this Creature c) => c.Template.type == CRIT_CT_GOLDCENTI || c.Template.type == CRIT_CT_GOLDLIZ || c.Template.type == CRIT_CT_GOLDSPITTER;
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
                var c0 = owner.owner.bodyChunks[ind + 1];
                //TODO: better attachment points
                Vector2 res = Vector2.Lerp(c0.lastPos, c0.pos, ts);
                return res;
            }
            internal Dangler.DanglerProps Props(int ind)
                => new()
                { gravity = -0.02f,
                    airFriction = 0.9f,
                    waterGravity = 0.03f,
                    elasticity = 0.1f,
                    waterFriction = 0.4f,
                    weightSymmetryTendency = 0.5f
                };
        }
        internal static readonly AttachedField<CentipedeGraphics, CentiGrafFields> centiFields = new();

        internal static void CRIT_Enable()
        {
            //templates
            foreach (var t in new[] { CRIT_CT_GOLDLIZ, CRIT_CT_GOLDCENTI, CRIT_CT_GOLDSPITTER })
            {
                var cgold = GetCreatureTemplate(t);
                cgold.baseDamageResistance *= CRIT_GOLDEN_RESIST_MODIFIER;
                cgold.shortcutColor = MartyrChar.echoGold;
                if (RemnantPlugin.DebugMode)
                {
                    LogWarning($"MODIFIED CREATURE TEMPLATE: {t}");
                    LogBunch(__arglist(cgold.baseDamageResistance));
                    LogWarning("~~ __ ~~");
                }
            }
            var glizdbreed = GetCreatureTemplate(CRIT_CT_GOLDLIZ).breedParameters as LizardBreedParams;
            glizdbreed.toughness = 300f;
            glizdbreed.bodySizeFac = 1.5f;
            glizdbreed.limbSize = 1.75f;
            glizdbreed.standardColor = HSL2RGB(0.13f, 1, 0.63f);
            glizdbreed.tailSegments = 19;
            glizdbreed.headSize = 1.5f;
            glizdbreed.tamingDifficulty = 9f;
            //liz
            On.Lizard.ctor += Liz_Recolor;
            On.LizardCosmetics.TailFin.DrawSprites += Liz_recolorTailFins;
            On.LizardCosmetics.TailFin.ctor += Liz_increaseFinSize;
            manualHooks.Add(new ILHook(ctorof<LizardGraphics>(typeof(PhysicalObject)), Liz_IL_makeGraphic));
            //manualHooks.Add(new Hook(methodof<LizardSpit>(nameof(LizardSpit.DrawSprites))))
            manualHooks.Add(new Hook(methodof<LizardSpit>("DrawSprites"), methodof(mhk_t, nameof(Liz_RecolorSpit))));
            //centi
            On.CentipedeGraphics.ctor += Centi_recolorShellReg;
            On.CentipedeGraphics.InitiateSprites += CentiG_makesprites;
            On.CentipedeGraphics.AddToContainer += CentiG_ATC;
            On.CentipedeGraphics.DrawSprites += CentiG_Draw;
            On.CentipedeGraphics.ApplyPalette += CentiG_APal;
            On.CentipedeGraphics.Update += CentiG_Update;
            On.CentipedeGraphics.WhiskerLength += Centi_EnlargeWhiskers;
            manualHooks.Add(new Hook(methodof<CentipedeAI>("Update"), methodof(mhk_t, nameof(CentiAI_Update))));
            manualHooks.Add(new Hook(methodof<Centipede>("ShortCutColor"), methodof(mhk_t, nameof(Centi_ShortCutColor))));
            manualHooks.Add(new ILHook(ctorof<Centipede>(typeof(AbstractCreature), typeof(World)), Centi_resize));
            manualHooks.Add(new Hook(methodof<Dangler>("ConPos"), methodof(mhk_t, nameof(EDA_conpos))));
            manualHooks.Add(new Hook(methodof<Dangler>("get_Props"), methodof(mhk_t, nameof(EDA_props))));
            manualHooks.Add(new Hook(methodof<Centipede>("Crawl"), methodof(mhk_t, nameof(Centi_Crawl))));
            //spitters
            On.BigSpider.Spit += shotgunBlast;
            On.DartMaggot.Shoot += RegSpitMultiplication;
            On.DartMaggot.ChangeMode += Darts_ReflectOffMartyr;
            //IL.DartMaggot.ShotUpdate += Dart_ReflectOffGhosts;
            On.BigSpider.ctor += makeSpitter;
            //On.BigSpiderGraphics.ctor += recolorSpitters;
            On.BigSpiderGraphics.DrawSprites += SpiderG_Draw;
            IL.BigSpiderAI.SpiderSpitModule.Update += Spider_slowReload;
            IL.BigSpiderGraphics.ApplyPalette += SpiderG_removeDarkness;
            IL.BigSpiderGraphics.ctor += SpiderG_RecolorAndFluff;
        }

        //private static void Dart_ReflectOffGhosts(ILContext il)
        //{
        //    ILCursor c = new(il);
        //    c.GotoNext(MoveType.Before,
        //        xx => xx.MatchLdloca(6),
        //        xx => xx.MatchLdfld<SharedPhysics.CollisionResult>("chunk"),
        //        xx => xx.MatchBrfalse(out _));
        //    //c.Emit(Ldarg_0);
        //    c.Emit(Ldloca, il.Body.Variables[6]);
        //    //c.EmitDelegate<Action<DartMaggot, SharedPhysics.CollisionResult>>((mag, cr) => {
        //    //    if (cr.chunk?.owner is Player p && playerFieldsByHash.ContainsKey(p.GetHashCode())) cr.chunk = null;
        //    //});
        //    var br = c.CurrentInstruction();
        //    c.EmitDelegate<Func<SharedPhysics.CollisionResult, bool>>(cr =>
        //    cr.chunk?.owner is Player p
        //    && playerFieldsByHash.TryGetValue(p.GetHashCode(), out var mf)
        //    && mf.echoActive);
        //}

        private static void Darts_ReflectOffMartyr(On.DartMaggot.orig_ChangeMode orig, DartMaggot self, DartMaggot.Mode newMode)
        {
            if (newMode == DartMaggot.Mode.StuckInChunk
                && self.stuckInChunk?.owner is Player p
                && playerFieldsByHash.TryGetValue(p.GetHashCode(), out var mf)
                && mf.echoActive) newMode = DartMaggot.Mode.Free;
            orig(self, newMode);
        }
        #region spider
        private static void SpiderG_RecolorAndFluff(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After, xx => xx.MatchStfld<BigSpiderGraphics>("scales"));
            c.Emit(Ldarg_0).EmitDelegate<Action<BigSpiderGraphics>>(spg =>
            {
                if (spg.bug.IsGolden())
                {
                    Array.Resize(ref spg.scales, spg.scales.Length + 14);
                    LogWarning("Extra fluff added! " + spg.scales.Length);
                }
            });
            c.GotoNext(MoveType.Before, xx => xx.MatchRet());
            c.Emit(Ldarg_0).EmitDelegate<Action<BigSpiderGraphics>>(spg => spg.yellowCol = spg.bug.IsGolden() ? spg.yellowCol : MartyrChar.echoGold);
        }

        private static void SpiderG_removeDarkness(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.Before, xx => xx.MatchRet());
            c.GotoPrev(MoveType.Before,
                //xx => xx.MatchLdarg(0),
                xx => xx.MatchLdfld<BigSpiderGraphics>("darkness"),
                xx => xx.MatchSub(),
                xx => xx.MatchMul()) ;
            c.Remove().EmitDelegate<Func<BigSpiderGraphics, float>>(spg => spg.bug.IsGolden() ? 0.1f : spg.darkness);
            //il.dump(RootFolderDirectory(), "spider_applypalette.txt");
        }
        private static void SpiderG_Draw(On.BigSpiderGraphics.orig_DrawSprites orig, BigSpiderGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            //if (self.bug.IsGolden()) self.darkness = 0f;
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }
        //private static void recolorSpitters(On.BigSpiderGraphics.orig_ctor orig, BigSpiderGraphics self, PhysicalObject ow)
        //{
        //    orig(self, ow);
        //    if (self.bug.IsGolden()) self.yellowCol = MartyrChar.echoGold;
        //}
        private static void Spider_slowReload(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After,
                xx => xx.MatchLdarg(0),
                xx => xx.MatchLdfld<BigSpiderAI.SpiderSpitModule>("fastAmmoRegen"),
                xx => xx.MatchBrfalse(out _));
            c.CurrentInstruction().Operand = 600f;
            c.GotoNext(MoveType.Before, xx => xx.MatchLdcR4(1200f));
            c.CurrentInstruction().Operand = 2400f;
        }
        private static BigSpider SpitterLock;
        private static DartMaggot MaggotToMultiply;
        private static void RegSpitMultiplication(On.DartMaggot.orig_Shoot orig, DartMaggot self, Vector2 pos, Vector2 dir, Creature shotBy)
        {
            orig(self, pos, dir, shotBy);
            if (shotBy == SpitterLock) MaggotToMultiply ??= self;
        }
        private static void shotgunBlast(On.BigSpider.orig_Spit orig, BigSpider self)
        {
            SpitterLock = self;
            orig(self);
            var bcen = MaggotToMultiply.firstChunk.pos;
            var bdir = MaggotToMultiply.needleDir;
            if (MaggotToMultiply != default)
            {
                for (int i = 0; i < URand.Range(3, 5); i++)
                {
                    AbstractPhysicalObject apo = new (
                        self.room.world, 
                        AbstractPhysicalObject.AbstractObjectType.DartMaggot, 
                        null, 
                        self.abstractCreature.pos, 
                        self.room.game.GetNewID());
                    self.room.abstractRoom.AddEntity(apo);
                    apo.destroyOnAbstraction = true;
                    apo.RealizeInRoom();
                    var rMaggot = apo.realizedObject as DartMaggot;
                    rMaggot.Shoot(
                        bcen + RNV() * 2.5f, 
                        RotateAroundOrigo(bdir, URand.Range(-7f, 7f)), 
                        SpitterLock);
                }
                for (int i = 0; i < URand.Range(2, 3); i++) {
                    Smoke.FireSmoke.FireSmokeParticle exhaust = new();
                    float dev = 3f + (float)i * 3f;
                    exhaust.Reset(null, bcen, RotateAroundOrigo(bdir, URand.Range(-dev, dev)), 14f);
                    exhaust.lifeTime = 14f;
                    exhaust.effectColor = (i is 0) ? Color.white : MartyrChar.echoGold;
                    exhaust.colorFadeTime = 5;
                    self.room.AddObject(exhaust);
                }
 
            }
            MaggotToMultiply = default;
            SpitterLock = default;
        }

        private static void makeSpitter(On.BigSpider.orig_ctor orig, BigSpider self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (self.IsGolden())
            {
                self.yellowCol = MartyrChar.echoGold;
            }
            
        }
        #endregion spider
        #region golden centi
        private static float Centi_EnlargeWhiskers(
            On.CentipedeGraphics.orig_WhiskerLength orig,
            CentipedeGraphics self,
            int part)
        {
            var res = orig(self, part);
            if (self.centipede.IsGolden()) res *= 1.35f;
            return res;
        }
        private static void CentiG_APal(
            On.CentipedeGraphics.orig_ApplyPalette orig,
            CentipedeGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (centiFields.TryGet(self, out var cf))
            {
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    sLeaser.sprites[i + cf.bStartSprite].color = self.ShellColor;
                }
            }
        }
        private static void CentiG_Update(
            On.CentipedeGraphics.orig_Update orig,
            CentipedeGraphics self)
        {
            orig(self);
            if (centiFields.TryGet(self, out var cf))
            {
                foreach (var d in cf.danglers) d.Update();
            }
        }
        private static void CentiG_Draw(
            On.CentipedeGraphics.orig_DrawSprites orig,
            CentipedeGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos)
        {

            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            else if (centiFields.TryGet(self, out var cf))
            {
                foreach (var d in cf.danglers) d.DrawSprite(cf.bStartSprite + d.danglerNum, sLeaser, rCam, timeStacker, camPos);
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    sLeaser.sprites[i + cf.bStartSprite].color = self.ShellColor;
                }
            }

        }
        private static void CentiG_ATC(
            On.CentipedeGraphics.orig_AddToContainer orig,
            CentipedeGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (CENTI_SIN_LOCK || !centiFields.TryGet(self, out var cf)) return;
            else
            {
                for (int i = 0; i < cf.danglerCount; i++)
                {
                    rCam.ReturnFContainer("Items").AddChild(sLeaser.sprites[i + cf.bStartSprite]);
                }
            }
        }
        internal static bool CENTI_SIN_LOCK = false;
        private static void CentiG_makesprites(
            On.CentipedeGraphics.orig_InitiateSprites orig,
            CentipedeGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam)
        {
            CENTI_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            CENTI_SIN_LOCK = false;
            if (centiFields.TryGet(self, out var cf))
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
        //ihavedanglers
        private static Dangler.DanglerProps EDA_props(Func<Dangler, Dangler.DanglerProps> orig, Dangler self)
        {
            if (self.gModule is CentipedeGraphics cg
                && centiFields.TryGet(cg, out var cf))
            {
                return cf.Props(self.danglerNum);
            }
            return orig(self);
        }
        private static Vector2 EDA_conpos(Func<Dangler, float, Vector2> orig, Dangler self, float timeStacker)
        {
            if (self.gModule is CentipedeGraphics cg
                && centiFields.TryGet(cg, out var cf))
            {
                return cf.ConPos(self.danglerNum, timeStacker);
            }
            return (orig(self, timeStacker));
        }
        //misc
        private static void CentiAI_Update(Action<CentipedeAI> orig, CentipedeAI self)
        {
            orig(self);
            self.idleCounter = Min(self.idleCounter, 3);
        }
        private static void Centi_Crawl(Action<Centipede> orig, Centipede self)
        {
            var oldsize = self.size;
            if (self.Red) self.size *= 0.1f;
            orig(self);
            self.size = oldsize;
        }
        private static void Centi_resize(ILContext il)
        {
            var c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, xx => xx.MatchCallOrCallvirt<PhysicalObject>("set_bodyChunks")))
            {
                LogWarning("CENTI RESIZE: FOUND INSERTION POINT");
                c.Emit(Ldarg_0);
                c.Emit(Ldarg_0);
                c.Emit(Ldfld, typeof(PhysicalObject).GetField(pbfiname(nameof(PhysicalObject.bodyChunks)), allContextsInstance));
                //c.Emit(ldfl)
                c.EmitDelegate<Action<Centipede, BodyChunk[]>>((cb, body)
                    => { if (!cb.Red) return;
                        Array.Resize(ref body, body.Length + 7);
                        cb.bodyChunks = body;
                        Log($"CENTI RESIZE: new bodychunk count is {cb.bodyChunks.Length}");
                    });
            }
            else
            {
                LogWarning("CENTI RESIZE: failed to find insertion point!");
            }
        }
        private static Color Centi_ShortCutColor(Func<Centipede, Color> orig, Centipede self) 
            => self.Red ? Color.yellow : orig(self);
        private static void Centi_recolorShellReg(
            On.CentipedeGraphics.orig_ctor orig,
            CentipedeGraphics self,
            PhysicalObject ow)
        {
            orig(self, ow);
            if (self.centipede.IsGolden())
            {
                self.saturation = 1f;
                self.hue = 0.13f;
                centiFields.Set(self, new CentiGrafFields(self));
            }
        }
        #endregion golden centi
        #region golden lizard

        private static void Liz_increaseFinSize(
            On.LizardCosmetics.TailFin.orig_ctor orig,
            LizardCosmetics.TailFin self,
            LizardGraphics lGraphics,
            int startSprite)
        {
            orig(self, lGraphics, startSprite);
            if (!lGraphics.lizard.IsGolden()) return;
            self.sizeRangeMin *= 1.1f;
            self.sizeRangeMax *= 1.35f;
            self.colored = true;
            self.numberOfSprites = ((!self.colored) ? self.bumps : (self.bumps * 2)) * 2;
        }
        private static void Liz_Recolor(
            On.Lizard.orig_ctor orig,
            Lizard self,
            AbstractCreature abstractCreature,
            World world)
        {
            orig(self, abstractCreature, world);
            if (self.IsGolden())
            {
                URand.seed = abstractCreature.ID.number;
                self.effectColor = Color.yellow.RandDev(new Color(0.125f, 0.09f, 0.08f));
            }
        }
        private static void Liz_recolorTailFins(
            On.LizardCosmetics.TailFin.orig_DrawSprites orig,
            LizardCosmetics.TailFin self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!self.lGraphics.lizard.IsGolden()) return;
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
        private static void Liz_IL_changeTemplate(ILContext il)
        {
#warning changes needed in case of separation from reds
            static bool li1(Instruction xx) => xx.MatchLdcR4(1);
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
                LogWarning("GOLDLIZTEMPLATE: CONN PATCH INSERTED");
            }
            else
            {
                LogWarning("GOLDLIZTEMPLATE: FAILED TO FIND INSERTION POINT!");
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
                LogWarning("GOLDLIZTEMPLATE: EXIT MOD APPLIED");
            }
            else
            {
                LogWarning("GOLDLIZTEMPLATE: FAILED TO APPLY EXIT MODIFICATION");
            }
        }
        private static void Liz_IL_makeGraphic(ILContext il)
        {
            var mynum = il.Body.Variables[11];
            var c = new ILCursor(il);
            c.GotoNext(ins0 => ins0.MatchLdarg(0),
                    ins1 => ins1.MatchLdarg(0),
                    ins2 => ins2.MatchLdfld(nameof(LizardGraphics), nameof(LizardGraphics.lizard)),
                    ins3 => ins3.MatchCallOrCallvirt<Creature>("get_mainBodyChunk"));
            var exitl = c.DefineLabel();
            var ex2 = c.Instrs[c.Index];
            if (RemnantPlugin.DebugMode) LogWarning($"exit defined");
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
                    if (self.lizard.IsGolden())
                    {
                        spr = self.AddCosmetic(spr, new LizardCosmetics.SpineSpikes(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.TailFin(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.LongShoulderScales(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.SpineSpikes(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.TailGeckoScales(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.JumpRings(self, spr));
                        spr = self.AddCosmetic(spr, new LizardCosmetics.ShortBodyScales(self, spr));
                        Debug.Log("GOLDLIZ cosmetics applied.");
                    }
                    return spr;
                });
                c.Emit(Stloc_S, mynum);
                c.Emit(Br, ex2);
                if (RemnantPlugin.DebugMode) LogWarning("GOLDLIZ: liz graphics ctor defiled successfully");
                //File.WriteAllText(Path.Combine(RootFolderDirectory(), "ild.txt"), il.ToString());
            }
            else
            {
                if (RemnantPlugin.DebugMode) LogWarning("GOLDLIZ: FAILED TO FIND INSERTION POINT!");
            }
        }
        private static void Liz_RecolorSpit(
            On.LizardSpit.orig_DrawSprites orig,
            LizardSpit self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[self.DotSprite].color = Color.yellow;
        }
        //private static void SpitRecolor
        #endregion golden lizard

        internal static void CRIT_Disable()
        {
            //liz
            On.Lizard.ctor -= Liz_Recolor;
            On.LizardCosmetics.TailFin.DrawSprites -= Liz_recolorTailFins;
            On.LizardCosmetics.TailFin.ctor -= Liz_increaseFinSize;
            //centi

            On.CentipedeGraphics.ctor -= Centi_recolorShellReg;
            On.CentipedeGraphics.InitiateSprites -= CentiG_makesprites;
            On.CentipedeGraphics.AddToContainer -= CentiG_ATC;
            On.CentipedeGraphics.DrawSprites -= CentiG_Draw;
            On.CentipedeGraphics.ApplyPalette -= CentiG_APal;
            On.CentipedeGraphics.Update -= CentiG_Update;
            On.CentipedeGraphics.ctor -= Centi_recolorShellReg;
            On.CentipedeGraphics.WhiskerLength -= Centi_EnlargeWhiskers;
            centiFields.Clear();

            //spider

            On.BigSpider.Spit -= shotgunBlast;
            On.DartMaggot.Shoot -= RegSpitMultiplication;
            On.BigSpider.ctor -= makeSpitter;
            IL.BigSpiderAI.SpiderSpitModule.Update -= Spider_slowReload;
            //On.BigSpiderGraphics.ctor -= recolorSpitters;
            On.BigSpiderGraphics.DrawSprites -= SpiderG_Draw;
            IL.BigSpiderGraphics.ApplyPalette -= SpiderG_removeDarkness;
            IL.BigSpiderGraphics.ctor -= SpiderG_RecolorAndFluff;
            //IL.DartMaggot.ShotUpdate -= Dart_ReflectOffGhosts;
            On.DartMaggot.ChangeMode -= Darts_ReflectOffMartyr;
        }
    }
}
