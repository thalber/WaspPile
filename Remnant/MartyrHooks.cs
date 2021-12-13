using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using MonoMod.RuntimeDetour;
using System.Reflection;
using WaspPile.Remnant.UAD;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static WaspPile.Remnant.RemnantUtils;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    public static partial class MartyrHooks 
    {
        //TODO: stats are pretty arbitrary, reach agreement

        internal const float ECHOMODE_DAMAGE_BONUS = 1.7f;
        internal const float ECHOMODE_THROWFORCE_BONUS = 1.4f;
        internal const float ECHOMODE_RUNSPEED_BONUS = 1.4f;
        internal const float ECHOMODE_DEPLETE_COOLDOWN = 270f;
        internal const float ECHOMODE_BUOYANCY_BONUS = 8f;
        internal const float ECHOMODE_WATERFRIC_BONUS = 1.1f;

        //represents additional martyr related fields for Player
        public class MartyrFields
        {
            //ability
            public float maxEchoReserve;
            public float rechargeRate;
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
            public float baseWaterFric;
            public Color palBlack = new Color(0.1f, 0.1f, 0.1f);
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

        public static void powerDown(this Player self, ref MartyrFields mf, bool fullDeplete = false)
        {
            Console.WriteLine("Martyr ability down");
            mf.cooldown = fullDeplete ? 
                ECHOMODE_DEPLETE_COOLDOWN 
                : ECHOMODE_DEPLETE_COOLDOWN * InverseLerp(mf.maxEchoReserve, 0f, mf.echoReserve) * 0.8f;
            Console.WriteLine($"cd: {mf.cooldown}");
            mf.echoActive = false;
            self.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.firstChunk.pos, 1.0f, 0.5f);
            self.lungsExhausted = true;
            self.airInLungs = fullDeplete ? 0f : 0.2f;
            
        }
        public static void powerUp(this Player self, ref MartyrFields mf)
        {
            Console.WriteLine($"Martyr ability up\nreserve: {mf.echoReserve}");
            mf.echoActive = true;
            self.room.PlaySound(SoundID.Rock_Hit_Creature, self.firstChunk.pos, 1.0f, 0.5f);
            self.airInLungs = 1f;
        }
        
        internal static readonly Dictionary<int, MartyrFields> fieldsByPlayerHash = new Dictionary<int, MartyrFields>();
        internal static readonly List<IDetour> manualHooks = new List<IDetour>();
        internal static readonly Dictionary<int, WeaponFields> poweredWeapons = new Dictionary<int, WeaponFields>();

        public static void Enable()
        {
            //lc
            On.RainWorldGame.ctor += GameStarts;
            On.Player.ctor += RegisterFieldset;
            On.Player.Update += RunAbilityCycle;

            //em
            //On.Player.ThrownSpear += EchomodeDamageBonus;
            On.Player.MovementUpdate += EchomodeExtendRoll;
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

            CRIT_Enable();
        }



        #region misc

        private static void PromptCycleWarning(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            abstractCreature.Room.realizedRoom?.AddObject(new CyclePrompt());
        }
        #endregion
        #region idrawable
        //initsprites lock, active when vanilla run of initsprites is in effect
        private static bool PLAYER_SIN_LOCK;

        private static void Player_ATC(On.PlayerGraphics.orig_AddToContainer orig, 
            PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (!fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf) || mf.bubbleSpriteIndex == -1 || PLAYER_SIN_LOCK) return;
            try
            {
                Console.WriteLine($"martyr addtocont: bubble indecks {mf.bubbleSpriteIndex} sleaser.s length {sLeaser.sprites.Length}");
                var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
                bubble.RemoveFromContainer();
                rCam.ReturnFContainer("HUD").AddChild(bubble);
            }
            catch (IndexOutOfRangeException) { Console.WriteLine("Something went bad on martyr player.ATC"); }
        }

        private static void Player_Draw(On.PlayerGraphics.orig_DrawSprites orig, 
            PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.slatedForDeletetion || self.owner.room != rCam.room || !fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf)) return;
            var npos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            
            //martyr bubble
            var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
            bubble.SetPosition(npos - camPos);
            //bubble.alpha = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.element = Futile.atlasManager.GetElementWithName("Futile_White");
            var cf = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.scale = Lerp(13f, 16f, cf);
            bubble.isVisible = URand.value < cf;
            
            //ability body color fade
            var currBodyCol = Color.Lerp(mf.lastBCol, mf.bCol, timeStacker);
            var currEyeCol = Color.Lerp(mf.lastECol, mf.eCol, timeStacker);
            for (int i = 0; i < 9; i++) sLeaser.sprites[i].color = currBodyCol;
            //sLeaser.sprites[9].color = currEyeCol;

            //face elm
            var face = sLeaser.sprites[9];
            face.color = currEyeCol;
            //var oldelm = face.element;
            if (mf.echoActive)
            {
                face.element = Futile.atlasManager.GetElementWithName(
                HUD.KarmaMeter.KarmaSymbolSprite(
                    true, new IntVector2(Min(9, (int)(mf.echoReserve / mf.maxEchoReserve * 10)), 9)));
                face.scale = 0.25f;
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

        private static void Player_MakeSprites(On.PlayerGraphics.orig_InitiateSprites orig, 
            PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PLAYER_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            PLAYER_SIN_LOCK = false;
            if (!fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf)) return;
            foreach (var sprite in sLeaser.sprites) sprite.RemoveFromContainer();
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
            mf.bubbleSpriteIndex = sLeaser.sprites.Length - 1;
            Console.WriteLine($"martyr bubble sprite: {mf.bubbleSpriteIndex}");
            sLeaser.sprites[mf.bubbleSpriteIndex] = new FSprite(Futile.atlasManager.GetElementWithName("Futile_White"));
            sLeaser.sprites[mf.bubbleSpriteIndex].shader = self.player.room.game.rainWorld.Shaders["GhostDistortion"];
            self.AddToContainer(sLeaser, rCam, null);
        }

        private static void Player_APal(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf))
            {
                mf.palBlack = palette.blackColor;
            }
        }
        #endregion
        #region ability

        private static void wallbang(Action<Weapon> orig,
            Weapon self)
        {
            orig(self);
            if (poweredWeapons.TryGetValue(self.GetHashCode(), out var wf))
            {
                self.room.AddObject(new Explosion(self.room,
                        sourceObject: self,
                        pos: self.firstChunk.pos,
                        lifeTime: 6,
                        rad: URand.Range(20f, 24f),
                        force: URand.Range(7f, 10f),
                        damage: 0.01f,
                        stun: URand.Range(0.4f, 1.1f),
                        deafen: 0.1f,
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
            if (poweredWeapons.TryGetValue(self.GetHashCode(), out var wf) && result.chunk != null)
            {
                self.room.AddObject(new Explosion(self.room,
                        sourceObject: self,
                        pos: self.firstChunk.pos,
                        lifeTime: 6,
                        rad: URand.Range(12f, 14f),
                        force: URand.Range(4f, 8.5f),
                        damage: 0.09f,
                        stun: URand.Range(0.12f, 0.18f),
                        deafen: 0.3f,
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
                Console.WriteLine("Smack!");
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

                for (int i = hit ? URand.Range(5, 8) : URand.Range(2, 3); i > 0; i--)
                {
                    Vector2 ppos = hit ? self.firstChunk.pos + RNV() * 10 : V2RandLerp(self.firstChunk.lastPos, self.firstChunk.pos);
                    Vector2 pvel = PerpendicularVector(self.firstChunk.lastLastPos - self.firstChunk.pos) *  RandSign();
                    var part = new Smoke.FireSmoke.FireSmokeParticle();
                    part.Reset(wf.fss, 
                        pos: hit? ppos : ppos + pvel * 5f, 
                        vel: hit? pvel : RNV() * URand.Range(10f, 15f), 
                        lifeTime: 9);
                    part.colorFadeTime = 12;
                    part.effectColor = RainWorld.GoldRGB;
                    self.room.AddObject(part);
                    //TODO: finalize particle spawning
                }
            }
        }
        private static void EchomodeExtendRoll(On.Player.orig_MovementUpdate orig, 
            Player self, bool eu)
        {
            //TODO: infinite slides
            orig(self, eu);
            if (!fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf)) return;
            //arbitrary threshold but works so far
            if (mf.echoActive && self.rollCounter > 30) self.rollCounter = 30;
            if (mf.echoActive && self.slideCounter > 5) self.slideCounter = 5;
        }

        private static void EchomodePreventDamage(On.Creature.orig_Violence orig, 
            Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, 
            PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player m
                && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf)
                && mf.echoActive
                && source?.owner is Spear) damage = 0f; 
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void EchomodeVelBonus(On.Weapon.orig_Thrown orig, 
            Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (thrownBy is Player m && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf) && mf.echoActive)
            {
                poweredWeapons.Add(self.GetHashCode(), new WeaponFields(mf.palBlack, self.room));
                foreach (var c in self.bodyChunks) c.vel *= ECHOMODE_THROWFORCE_BONUS;
                if (!(self is Spear spear)) return;
                spear.spearDamageBonus *= ECHOMODE_DAMAGE_BONUS;
            }
        }
        
        private static bool EchomodeDeflection(On.Creature.orig_SpearStick orig, 
            Creature self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            if (self is Player m && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) return false;
            }
            return orig(self, source, dmg, chunk, appPos, direction);
        }
        
        //private static void EchomodeDamageBonus(On.Player.orig_ThrownSpear orig, 
        //    Player self, Spear spear)
        //{
        //    orig(self, spear);
        //}
        #endregion
        #region lifecycle
        private static void RunAbilityCycle(On.Player.orig_Update orig, 
            Player self, bool eu)
        {
            if (!fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf)) goto skipNotMine;
            //basic recharge/cooldown and activation
            mf.lastKeyDown = mf.keyDown;
            mf.keyDown = Input.GetKeyDown(RemnantConfig.GetKeyForPlayer(self.room.game.Players.IndexOf(self.abstractCreature)));
            bool toggleRequested = (mf.keyDown && !mf.lastKeyDown);
            if (toggleRequested) Console.WriteLine("Martyr toggle req");
            if (mf.echoActive)
            {
                mf.echoReserve -= 1f;
                if (mf.echoReserve < 0 || toggleRequested)
                {
                    self.powerDown(ref mf, mf.echoReserve < 0);
                }
            }
            else
            {
                mf.echoReserve = Min(mf.maxEchoReserve, mf.echoReserve + mf.rechargeRate);
                mf.cooldown = Max(0, mf.cooldown - 1f);
                if (toggleRequested && mf.cooldown == 0) self.powerUp(ref mf);
                if (toggleRequested && mf.cooldown != 0) self.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, self.firstChunk.pos, 0.9f, 0.45f);
            }

            //basic stats modification
            self.slugcatStats.runspeedFac = mf.echoActive 
                ? mf.baseRunSpeed * ECHOMODE_RUNSPEED_BONUS 
                : mf.baseRunSpeed;
            self.buoyancy = mf.echoActive 
                ? mf.baseBuoyancy * ECHOMODE_BUOYANCY_BONUS 
                : mf.baseBuoyancy;
            self.waterFriction = mf.echoActive 
                ? mf.baseWaterFric * ECHOMODE_WATERFRIC_BONUS 
                : mf.baseWaterFric;

            //visuals
            mf.lastFade = mf.fade;
            mf.fade = Custom.LerpAndTick(mf.fade, mf.echoActive ? 1f : 0f, 0.08f, 0.04f);
            mf.lastBCol = mf.bCol;
            mf.lastECol = mf.eCol;
            mf.bCol = Color.Lerp(MartyrChar.deplBodyCol, MartyrChar.baseBodyCol, mf.echoReserve / mf.maxEchoReserve);
            mf.eCol = Color.Lerp(MartyrChar.deplEyeCol, MartyrChar.baseEyeCol, mf.echoReserve / mf.maxEchoReserve);
            skipNotMine:
            orig(self, eu);
        }
        
        private static void RegisterFieldset(On.Player.orig_ctor orig, 
            Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            fieldsByPlayerHash.Add(self.GetHashCode(), new MartyrFields()
            {
                maxEchoReserve = 520f,
                echoReserve = 520f,
                rechargeRate = 0.8f,
                baseBuoyancy = self.buoyancy,
                baseRunSpeed = self.slugcatStats.runspeedFac,
                baseWaterFric = self.waterFriction,
                echoActive = false,
                fade = 0f,
                bubbleSpriteIndex = -1,
                bCol = MartyrChar.baseBodyCol,
                lastBCol = MartyrChar.baseBodyCol
            }) ;
            //if (self.room.game.IsStorySession) self.redsIllness = new RedsIllness(self, Abs(RedsIllness.RedsCycles(false) - self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber));
        }
        
        private static void GameStarts(On.RainWorldGame.orig_ctor orig, 
            RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            fieldsByPlayerHash.Clear();
            poweredWeapons.Clear();
        }
        #endregion

        public static void Disable()
        {
            CRIT_Disable();

            On.RainWorldGame.ctor -= GameStarts;
            On.Player.ctor -= RegisterFieldset;
            On.Player.Update -= RunAbilityCycle;

            On.Weapon.Update -= flightVfx;
            //On.Player.ThrownSpear -= EchomodeDamageBonus;
            On.Creature.SpearStick -= EchomodeDeflection;
            On.Weapon.Thrown -= EchomodeVelBonus;
            On.Creature.Violence -= EchomodePreventDamage;
            On.Player.MovementUpdate -= EchomodeExtendRoll;

            On.PlayerGraphics.InitiateSprites -= Player_MakeSprites;
            On.PlayerGraphics.AddToContainer -= Player_ATC;
            On.PlayerGraphics.DrawSprites -= Player_Draw;
            On.PlayerGraphics.ApplyPalette -= Player_APal;

            On.Player.ctor -= PromptCycleWarning;

            foreach (var h in manualHooks) { h.Undo(); }
            manualHooks.Clear();
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StaticWorld).TypeHandle);
        }

        private static readonly Type mhk_t = typeof(MartyrHooks);
    }
}
