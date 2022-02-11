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
        //TODO: stats are pretty arbitrary, reach agreement
        internal const float ECHOMODE_DAMAGE_BONUS = 30f;
        internal const float ECHOMODE_THROWFORCE_BONUS = 1.4f;
        internal const float ECHOMODE_RUNSPEED_BONUS = 1.7f;
        internal const float ECHOMODE_CRAWLSPEED_BONUS = 1.4f;
        internal const float ECHOMODE_DEPLETE_COOLDOWN = 270f;
        internal const float ECHOMODE_BUOYANCY_BONUS = 8f;
        internal const float ECHOMODE_WATERFRIC_BONUS = 1.1f;

        //represents additional martyr related fields for Player
        public class MartyrFields
        {
            //ability
            public float maxEchoReserve;
            public float rechargeRate;
            public float effRechargeRate => rechargeRate * (inVoid ? 5f : 1f);
            public float depleteRate = 1f;
            public float effDepleteRate => depleteRate * (inVoid ? 0.5f : 1f);
            public float echoReserve;
            public bool keyDown;
            public bool lastKeyDown;
            //cooldown after depletion turning off, ticks down by 1f every frame
            public float cooldown;
            public bool echoActive;

            //toggle fade
            public float lastFade;
            public float fade;

            //bodycol
            public Color bCol;
            public Color lastBCol;
            public Color eCol;
            public Color lastECol;
            
            //service
            public int bubbleSpriteIndex;
            public float baseBuoyancy;
            public float baseRunSpeed;
            public float baseScootSpeed;
            public float basePoleSpeed;
            public float baseWaterFric;
            public Color palBlack = new(0.1f, 0.1f, 0.1f);
            public bool inVoid;
        }
        //same for martyr spears
        public class WeaponFields
        {
            public WeaponFields(Color rpbc, Room room)
            {
                black = rpbc;
                fss = new Smoke.FireSmoke(room);
            }
            public Smoke.FireSmoke fss;
            public Color black;
            public bool disableNextFrame = false;
        }

        internal const int ECHOMODE_RCB_SLIDE = 8;
        internal const int ECHOMODE_RCB_ROLL = 14;
        public static void powerDown(this Player self, ref MartyrFields mf, bool fullDeplete = false)
        {
            if (mf is null) return;
            mf.cooldown = fullDeplete ?
                ECHOMODE_DEPLETE_COOLDOWN
                : ECHOMODE_DEPLETE_COOLDOWN * InverseLerp(mf.maxEchoReserve, 0f, mf.echoReserve) * 0.8f;
            if (RemnantPlugin.DebugMode)
            {
                Console.WriteLine($"cd: {mf.cooldown}");
                Console.WriteLine("Martyr ability down");
            }
            
            mf.echoActive = false;
            self.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.firstChunk.pos, 1.0f, 0.5f);
            self.lungsExhausted |= mf.echoReserve / mf.maxEchoReserve < 0.35f;
            self.AerobicIncrease(fullDeplete ? 0.9f : Lerp((1 - mf.echoReserve / mf.maxEchoReserve), 0f, 0.2f)) ;
            self.airInLungs = fullDeplete ? 0f : Lerp(mf.echoReserve / mf.maxEchoReserve, 1f, 0.1f);
            
        }
        public static void powerUp(this Player self, ref MartyrFields mf)
        {
            if (!self.Consious || mf is null) return;
            mf.echoActive = true;
            self.room.PlaySound(SoundID.Rock_Hit_Creature, self.firstChunk.pos, 1.0f, 0.5f);
            self.airInLungs = 1f;
            if (RemnantPlugin.DebugMode) Console.WriteLine($"Martyr ability up\nreserve: {mf.echoReserve}");
        }

        internal static readonly List<IDetour> manualHooks = new();
        internal static readonly Dictionary<int, MartyrFields> playerFieldsByHash = new();
        internal static readonly Dictionary<int, WeaponFields> poweredWeapons = new();

        public static void Enable()
        {
            //lc
            On.RainWorldGame.ctor += GameStarts;
            On.Player.ctor += initFields;
            On.Player.Update += RunAbilityCycle;
            On.Player.Grabability += triSpearWield;
            On.Player.SetMalnourished += regstats;

            //em
            On.Player.ThrownSpear += EchomodeDamageBonusActual;
            IL.Player.MovementUpdate += IL_EchomodeClampRollc;
            On.Creature.SpearStick += EchomodeDeflection;
            On.Weapon.Thrown += EchomodeVelBonus;
            On.Creature.Violence += EchomodePreventDamage;
            On.Weapon.Update += flightVfx;
            foreach(Type t in new[] { typeof(Spear), typeof(Rock), typeof(WaterNut), typeof(Weapon) })
            {
                try
                {
                    var hs = t.GetMethod("HitSomething", allContextsInstance);
                    var hw = t.GetMethod("HitWall", allContextsInstance);
                    if (hs != null) manualHooks.Add(new Hook(hs,
                        mhk_t.GetMethod(nameof(extraPunch), allContextsStatic)));
                    if (hw != null) manualHooks.Add(new Hook(t.GetMethod("HitWall", allContextsInstance), 
                        mhk_t.GetMethod(nameof(wallbang), allContextsStatic)));
                }
                catch (Exception e)
                {
                    Debug.Log("couldn't register extra kick: " + e.Message);
                }
            }
            //id
            On.PlayerGraphics.ApplyPalette += Player_APal;
            On.PlayerGraphics.InitiateSprites += Player_MakeSprites;
            On.PlayerGraphics.AddToContainer += Player_ATC;
            On.PlayerGraphics.DrawSprites += Player_Draw;
            //misc
            On.Player.ctor += PromptCycleWarning;
            manualHooks.Add(new ILHook(methodof<Player>("EatMeatUpdate"), IL_GoldCure));
            On.SlugcatStats.SlugcatFoodMeter += slugFoodMeter;
            On.Player.Die += regdeath;
            CRIT_Enable();
            CONVO_Enable();
            WORLD_Enable();
        }

        #region misc
        private static void KillMoon(ILContext il)
        {
            ILCursor c = new(il);
            c.GotoNext(MoveType.After,
                xx => xx.MatchCallOrCallvirt<MiscWorldSaveData>("get_SLOracleState"),
                xx => xx.MatchCallOrCallvirt<SLOrcacleState>("get_neuronsLeft"),
                xx => xx.MatchStfld<Oracle>("glowers"));
            c.Emit(Ldarg_0).EmitDelegate<Action<Oracle>>(moon => {
                moon.health = 0f;
                moon.glowers = 0;
                if (moon.room.game.session is StoryGameSession ss) ss.saveState.miscWorldSaveData.SLOracleState.neuronsLeft = 0;
            });
        }

        private static void regdeath(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            if (self.TryGetSave<MartyrChar.MartyrSave>(out var css))
            {
                css.RemedyCache = false;
            }
        }

        private static IntVector2 slugFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, int slugcatNum) => new(9, 7);
        private static void IL_GoldCure(ILContext il)
        {
            var c = new ILCursor(il);
            //il.dump(RootFolderDirectory(), "whatever.txt");
            c.GotoNext(MoveType.After,
                xx => xx.MatchCallOrCallvirt<Player>("AddFood"));
            c.Emit(Ldarg_0);
            c.EmitDelegate<Action<Player>>(p => {
                if (p.grasps[0]?.grabbed is Creature cr 
                && cr.IsGolden() 
                && p.TryGetSave<MartyrChar.MartyrSave>(out var css))
                {
                    css.RemedyCache = true;
                }});
        }
        private static void PromptCycleWarning(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (abstractCreature.world.game.Players.IndexOf(abstractCreature) == -1) return;
            abstractCreature.Room.realizedRoom?.AddObject(new CyclePrompt());
        }
        #endregion
        #region idrawable
        //initsprites lock, active when vanilla run of initsprites is in effect
        private static bool PLAYER_SIN_LOCK;

        private static void Player_ATC(
            On.PlayerGraphics.orig_AddToContainer orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (!playerFieldsByHash.TryGetValue(self.player.GetHashCode(), out var mf) || mf.bubbleSpriteIndex == -1 || PLAYER_SIN_LOCK) return;
            try
            {
                if (RemnantPlugin.DebugMode) LogWarning($"martyr addtocont: bubble indecks {mf.bubbleSpriteIndex} sleaser.s length {sLeaser.sprites.Length}");
                var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
                bubble.RemoveFromContainer();
                rCam.ReturnFContainer("HUD").AddChild(bubble);
            }
            catch (IndexOutOfRangeException) { LogWarning("Something went bad on martyr player.ATC"); }
        }

        internal static void ExtendSprites(this PlayerGraphics self, MartyrFields mf, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            foreach (var sprite in sLeaser.sprites) sprite.RemoveFromContainer();
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
            mf.bubbleSpriteIndex = sLeaser.sprites.Length - 1;
            if (RemnantPlugin.DebugMode) LogWarning($"martyr bubble sprite: {mf.bubbleSpriteIndex}");
            sLeaser.sprites[mf.bubbleSpriteIndex] = new FSprite(Futile.atlasManager.GetElementWithName("Futile_White"))
            {
                shader = self.player.room.game.rainWorld.Shaders["GhostDistortion"]
            };
            self.AddToContainer(sLeaser, rCam, null);
        }
        private static void Player_Draw(
            On.PlayerGraphics.orig_DrawSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            float timeStacker,
            Vector2 camPos)
        {
            playerFieldsByHash.TryGetValue(self.player.GetHashCode(), out var mf);
            var face = sLeaser.sprites[9];
            face.scale = 1f;
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.slatedForDeletetion || self.owner.room != rCam.room || mf == null) return;
            //crutch for multiinst arena mode
            if (mf.bubbleSpriteIndex is default(int))
            {
                self.ExtendSprites(mf, sLeaser, rCam);
            }
            var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
            var npos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            
            //martyr bubble
            bubble.SetPosition(npos - camPos);
            //bubble.alpha = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.element = Futile.atlasManager.GetElementWithName("Futile_White");
            var cf = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.scale = Lerp(7f, 10f, cf);
            bubble.isVisible = URand.value < cf;
            
            //ability body color fade
            var currBodyCol = Color.Lerp(mf.lastBCol, mf.bCol, timeStacker);
            var currEyeCol = Color.Lerp(mf.lastECol, mf.eCol, timeStacker);
            for (int i = 0; i < 9; i++) sLeaser.sprites[i].color = currBodyCol;
            //sLeaser.sprites[9].color = currEyeCol;

            //face elm
            face.color = currEyeCol;
            //var oldelm = face.element;
            if (mf.echoActive)
            {
                face.element = Futile.atlasManager.GetElementWithName(
                HUD.KarmaMeter.KarmaSymbolSprite(
                    true, new IntVector2(Min(9, (int)(mf.echoReserve / mf.maxEchoReserve * 10)), 9)));
                face.scale = 0.35f;
                face.x = Round(face.x);
                face.y = Round(face.y);
            }
            else
            {
                face.scaleY = 1f;
                //face.scaleX = Sign(face.scaleX);
                //face.element = oldelm;
            }
            
        }
        private static void Player_MakeSprites(
            On.PlayerGraphics.orig_InitiateSprites orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam)
        {
            PLAYER_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            PLAYER_SIN_LOCK = false;
            if (!playerFieldsByHash.TryGetValue(self.player.GetHashCode(), out var mf)) return;
            self.ExtendSprites(mf, sLeaser, rCam);
        }

        private static void Player_APal(
            On.PlayerGraphics.orig_ApplyPalette orig,
            PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam,
            RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (playerFieldsByHash.TryGetValue(self.player.GetHashCode(), out var mf))
            {
                mf.palBlack = palette.blackColor;
            }
        }
        #endregion
        #region ability

        private static void EchomodeDamageBonusActual(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (playerFieldsByHash.TryGetValue(self.GetHashCode(), out var mf) && mf.echoActive)
            {
                spear.spearDamageBonus *= ECHOMODE_DAMAGE_BONUS;
            }
        }
        private static int triSpearWield(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (playerFieldsByHash.TryGetValue(self.GetHashCode(), out _) && obj is Spear) return (int)Player.ObjectGrabability.OneHand;
            return orig(self, obj);
        }
        private static void wallbang(Action<Weapon> orig,
            Weapon self)
        {
            orig(self);
            if (poweredWeapons.TryGetValue(self.GetHashCode(), out _))
            {
                self.room.AddObject(new Explosion(self.room,
                        sourceObject: self,
                        pos: self.firstChunk.pos,
                        lifeTime: 6,
                        rad: URand.Range(20f, 24f),
                        force: URand.Range(7f, 10f),
                        damage: 0.01f,
                        stun: URand.Range(0.4f, 1.1f),
                        deafen: 0f,
                        killTagHolder: self.thrownBy,
                        killTagHolderDmgFactor: 0.3f,
                        minStun: 0.01f,
                        backgroundNoise: 0.5f
                        ));
                self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos,
                    rad: URand.Range(26f, 30f),
                    alpha: URand.Range(0.65f, 0.73f),
                    lifeTime: 5,
                    lightColor: RainWorld.GoldRGB));
                self.room.PlaySound(SoundID.Gate_Water_Steam_Puff, self.firstChunk.pos, 0.8f, 1.2f);
            }
        }
        private static bool extraPunch(Func<Weapon, SharedPhysics.CollisionResult, bool, bool> orig, 
            Weapon self, SharedPhysics.CollisionResult result, bool eu)
        {
            //TODO: improve impact, smoke hit to separate uad?
            var res = orig(self, result, eu);
            if (poweredWeapons.TryGetValue(self.GetHashCode(), out _) && result.chunk != null)
            {
                self.room.AddObject(new Explosion(self.room,
                        sourceObject: self,
                        pos: self.firstChunk.pos,
                        lifeTime: 6,
                        rad: URand.Range(12f, 14f),
                        force: URand.Range(4f, 8.5f),
                        damage: 0.09f,
                        stun: URand.Range(0.12f, 0.18f),
                        deafen: 0f,
                        killTagHolder: self.thrownBy,
                        killTagHolderDmgFactor: 0.3f,
                        minStun: 0.02f,
                        backgroundNoise: 0.3f
                        ));
                self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos,
                    rad: URand.Range(26f, 30f),
                    alpha: URand.Range(0.65f, 0.73f),
                    lifeTime: 5,
                    lightColor: RainWorld.GoldRGB));
                self.room.PlaySound(SoundID.Fire_Spear_Explode, self.firstChunk.pos, 1.1f, 3.7f);
                self.room.ScreenMovement(self.firstChunk.pos, default, 1.4f);
                if (RemnantPlugin.DebugMode) Log("Smack!");
            }
            return res;
        }

        private static void flightVfx(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig(self, eu);
            if (poweredWeapons.TryGetValue(self.GetHashCode(), out var wf))
            {
                if (wf.disableNextFrame)
                {
                    poweredWeapons.Remove(self.GetHashCode());
                    return;
                }

                bool hit = false;
                if (self.mode != Weapon.Mode.Thrown)
                {
                    //TODO: make custom nondamaging explosion? tune either way
                    
                    wf.disableNextFrame = true;
                    hit = true;
                }

                for (int i = hit ? URand.Range(5, 8) : URand.Range(1, 2); i > 0; i--)
                {
                    Vector2 ppos = hit ? self.firstChunk.pos + RNV() * 10 : V2RandLerp(self.firstChunk.lastPos, self.firstChunk.pos);
                    Vector2 pvel = PerpendicularVector(self.firstChunk.lastLastPos - self.firstChunk.pos) *  RandSign();
                    Smoke.FireSmoke.FireSmokeParticle part = new ();
                    part.Reset(wf.fss, 
                        pos: hit? ppos : ppos + pvel * 5f, 
                        vel: hit? pvel : RNV() * URand.Range(10f, 15f), 
                        lifeTime: 9);
                    part.colorFadeTime = 12;
                    part.effectColor = MartyrChar.baseBodyCol;
                    self.room.AddObject(part);
                    //TODO: finalize particle spawning
                }
            }
        }
        //private static void IL_EchomodeEnsureWhiplash(ILContext il)
        //{
        //    var c = new ILCursor(il);
        //    c.GotoNext(MoveType.After, xx => xx.MatchLdarg(0), xx => xx.MatchLdcI4(1), xx => xx.MatchStfld<Player>("whiplashJump"));
        //    c.Emit(Ldarg_0);
        //    c.EmitDelegate<Action<Player>>(pl =>
        //    {
        //        if (playerFieldsByHash.TryGetValue(playerFieldsByHash.GetHashCode(), out var mf)
        //        && mf.echoActive
        //        && pl.rollCounter > 5
        //        && pl.rollDirection == -pl.input[0].x)
        //            pl.whiplashJump = true;
        //    });
        //}
        private static void IL_EchomodeClampRollc(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After, xx => xx.MatchStfld<Player>("rollCounter"));
            c.Emit(Ldarg_0);
            c.EmitDelegate<Action<Player>>(pl => { 
                if (playerFieldsByHash.TryGetValue(pl.GetHashCode(), out var mf) && mf.echoActive)
                {
                    int boundary = default;
                    switch (pl.animation)
                    {
                        case Player.AnimationIndex.BellySlide: boundary = ECHOMODE_RCB_SLIDE; break;
                        case Player.AnimationIndex.Roll: boundary = ECHOMODE_RCB_ROLL; break;
                    };
                    pl.rollCounter = Min(pl.rollCounter, boundary);
                }
            });
        }
        private static void EchomodeExtendRoll(On.Player.orig_UpdateAnimation orig, 
            Player self)
        {
#warning breaks whiplashes
            orig(self);
            if (!playerFieldsByHash.TryGetValue(self.GetHashCode(), out _)) return;
            int bound = default;
            switch (self.animation)
            {
                case Player.AnimationIndex.BellySlide: bound = 10; break;
                case Player.AnimationIndex.Roll: bound = 20; break;
            }
            self.rollCounter = Min(self.rollCounter, bound);
        }

        private static void EchomodePreventDamage(On.Creature.orig_Violence orig, 
            Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, 
            PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player m
                && playerFieldsByHash.TryGetValue(m.GetHashCode(), out var mf)
                && mf.echoActive
                && source?.owner is Spear) damage = 0f; 
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void EchomodeVelBonus(On.Weapon.orig_Thrown orig, 
            Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (thrownBy is Player m && playerFieldsByHash.TryGetValue(m.GetHashCode(), out var mf) && mf.echoActive)
            {
                poweredWeapons.Add(self.GetHashCode(), new WeaponFields(mf.palBlack, self.room));
                foreach (var c in self.bodyChunks) c.vel *= ECHOMODE_THROWFORCE_BONUS;
                if (self is not Spear spear) return;
                spear.spearDamageBonus *= ECHOMODE_DAMAGE_BONUS;
            }
        }
        
        private static bool EchomodeDeflection(On.Creature.orig_SpearStick orig, 
            Creature self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            if (self is Player m && playerFieldsByHash.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) return false;
            }
            return orig(self, source, dmg, chunk, appPos, direction);
        }
        #endregion
        #region lifecycle
        private static void regstats(On.Player.orig_SetMalnourished orig, Player self, bool m)
        {
            orig(self, m);
            if (playerFieldsByHash.TryGetValue(self.GetHashCode(), out var mf))
            {
                mf.baseBuoyancy = self.buoyancy;
                mf.baseRunSpeed = self.slugcatStats.runspeedFac;
                mf.baseScootSpeed = self.slugcatStats.corridorClimbSpeedFac;
                mf.basePoleSpeed = self.slugcatStats.poleClimbSpeedFac;
                mf.baseWaterFric = self.waterFriction;
            }
            if (RemnantPlugin.DebugMode)
            {
                //LogWarning("stamina pass 2");
                LogWarning(Json.Serialize(InstFieldsToDict(self.slugcatStats)));
            }
        }
        private static void RunAbilityCycle(On.Player.orig_Update orig, 
            Player self, bool eu)
        {
            if (!playerFieldsByHash.TryGetValue(self.GetHashCode(), out var mf))
            {
                if (self.room.game.IsArenaSession && MartyrChar.ME.IsMe(self)) self.RegMartyrInst();
                goto skipNotMine;
            }
            //basic recharge/cooldown and activation
            mf.lastKeyDown = mf.keyDown;
            mf.keyDown = Input.GetKey(RemnantConfig.GetKeyForPlayer(self.room.game.Players.IndexOf(self.abstractCreature)));
#warning change to any room with voidsea?
            mf.inVoid = self.room.abstractRoom.name == "SB_L01";
            bool toggleRequested = (mf.keyDown && !mf.lastKeyDown);
            if (RemnantPlugin.DebugMode && toggleRequested) LogWarning("Martyr toggle req");
            if (mf.echoActive)
            {
                self.aerobicLevel = 0f;
                self.airInLungs = 1f;
                mf.echoReserve -= mf.effDepleteRate;
                if (mf.echoReserve < 0 || toggleRequested || !self.Consious)
                {
                    self.powerDown(ref mf, mf.echoReserve < 0);
                }
            }
            else
            {
                mf.echoReserve = Min(mf.maxEchoReserve, mf.echoReserve + mf.effRechargeRate);
                mf.cooldown = Max(0, mf.cooldown - 1f);
                if (toggleRequested && mf.cooldown == 0) self.powerUp(ref mf);
                if (toggleRequested && mf.cooldown != 0) self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk.pos, 0.9f, 0.45f);
            }

            //basic stats modification
            self.slugcatStats.runspeedFac = mf.echoActive 
                ? mf.baseRunSpeed * ECHOMODE_RUNSPEED_BONUS 
                : mf.baseRunSpeed;
            self.buoyancy = mf.echoActive && !mf.inVoid
                ? mf.baseBuoyancy * ECHOMODE_BUOYANCY_BONUS 
                : mf.baseBuoyancy;
            self.waterFriction = mf.echoActive && !mf.inVoid 
                ? mf.baseWaterFric * ECHOMODE_WATERFRIC_BONUS 
                : mf.baseWaterFric;
            self.slugcatStats.poleClimbSpeedFac = mf.echoActive
                ? mf.basePoleSpeed * ECHOMODE_CRAWLSPEED_BONUS
                : mf.basePoleSpeed;
            self.slugcatStats.corridorClimbSpeedFac = mf.echoActive
                ? mf.baseScootSpeed * ECHOMODE_CRAWLSPEED_BONUS
                : mf.baseScootSpeed;
            //visuals
            mf.lastFade = mf.fade;
            mf.fade = LerpAndTick(mf.fade, mf.echoActive ? 1f : 0f, 0.08f, 0.04f);
            mf.lastBCol = mf.bCol;
            mf.lastECol = mf.eCol;
            mf.bCol = Color.Lerp(MartyrChar.deplBodyCol, MartyrChar.baseBodyCol, 1 - self.aerobicLevel);
            mf.eCol = Color.Lerp(MartyrChar.deplEyeCol, MartyrChar.baseEyeCol, mf.echoReserve / mf.maxEchoReserve);
            skipNotMine:
            orig(self, eu);
        }
        private static void initFields(On.Player.orig_ctor orig, 
            Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);

            if (!MartyrChar.ME?.IsMe(self) ?? true) return;
            self.RegMartyrInst();
        }
        internal static void RegMartyrInst(this Player self)
        {
            LogWarning("Instantiating a new martyr...");
            if (RemnantPlugin.DebugMode)
            {
            }
            self.spearOnBack = new Player.SpearOnBack(self);
            bool remedy = false;
            if (self.TryGetSave<MartyrChar.MartyrSave>(out var css))
            {
                remedy |= css.RemedyCache;
            }
            playerFieldsByHash.Add(self.GetHashCode(), new MartyrFields()
            {
                maxEchoReserve = 520f,
                echoReserve = 520f,
                rechargeRate = 0.8f,
                baseBuoyancy = self.buoyancy,
                baseRunSpeed = self.slugcatStats.runspeedFac,
                baseScootSpeed = self.slugcatStats.corridorClimbSpeedFac,
                basePoleSpeed = self.slugcatStats.poleClimbSpeedFac,
                baseWaterFric = self.waterFriction,
                echoActive = false,
                fade = 0f,
                bubbleSpriteIndex = -1,
                bCol = MartyrChar.baseBodyCol,
                lastBCol = MartyrChar.baseBodyCol
            });
            self.SetMalnourished(self.Malnourished || !remedy);
        }
        private static void GameStarts(On.RainWorldGame.orig_ctor orig, 
            RainWorldGame self, ProcessManager manager)
        {
            FieldCleanup();
            orig(self, manager);
            if (self.TryGetSave<MartyrChar.MartyrSave>(out _))
            {
                
            }
        }
        #endregion

        public static void Disable()
        {
            CRIT_Disable();
            CONVO_Disable();
            WORLD_Disable();
            On.RainWorldGame.ctor -= GameStarts;
            On.Player.ctor -= initFields;
            On.Player.Update -= RunAbilityCycle;
            On.Player.Die -= regdeath;
            On.Player.Grabability -= triSpearWield;
            On.Player.SetMalnourished -= regstats;

            On.Player.ThrownSpear -= EchomodeDamageBonusActual;
            On.Weapon.Update -= flightVfx;
            //On.Player.ThrownSpear -= EchomodeDamageBonus;
            On.Creature.SpearStick -= EchomodeDeflection;
            On.Weapon.Thrown -= EchomodeVelBonus;
            On.Creature.Violence -= EchomodePreventDamage;
            //On.Player.MovementUpdate -= EchomodeExtendRoll;
            IL.Player.MovementUpdate -= IL_EchomodeClampRollc;

            On.PlayerGraphics.InitiateSprites -= Player_MakeSprites;
            On.PlayerGraphics.AddToContainer -= Player_ATC;
            On.PlayerGraphics.DrawSprites -= Player_Draw;
            On.PlayerGraphics.ApplyPalette -= Player_APal;

            On.Player.ctor -= PromptCycleWarning;
            On.SlugcatStats.SlugcatFoodMeter -= slugFoodMeter;

            foreach (var h in manualHooks) { h.Undo(); }
            manualHooks.Clear();
            //System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StaticWorld).TypeHandle);
            //maybe this will work?
            typeof(StaticWorld).TypeInitializer?.Invoke(null, null);
        }

        internal static void FieldCleanup()
        {
            centiFields.Clear();
            playerFieldsByHash.Clear();
            poweredWeapons.Clear();
        }

        private static readonly Type mhk_t = typeof(MartyrHooks);
    }
}
