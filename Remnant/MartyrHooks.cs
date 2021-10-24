using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using static UnityEngine.Mathf;
using MonoMod.RuntimeDetour;
using System.Reflection;

//todo
//+base ability lifecycle (improve triggering)
//+visuals (bubble container problems)
//+throwforce and damage (extra indication?)
//+deflect
//+unhooking
//?waterbounce(crude but origo seems same)
//+ac/deac sounds
//+soft fade? replaced with flicker
//+em: infinite rolls
//?import art (remove asset dupes, move to ER)
//-testing
//+cycle limit?
//?one sitting option (mostly done, make sure early quit doesn't count either)

namespace WaspPile.Remnant
{

    public static class MartyrHooks 
    {
#warning stats are pretty arbitrary, sync
#warning add customizable ability bind

        const float ECHOMODE_DAMAGE_BONUS = 1.7f;
        const float ECHOMODE_THROWFORCE_BONUS = 1.8f;
        const float ECHOMODE_RUNSPEED_BONUS = 1.4f;
        const float ECHOMODE_DEPLETE_COOLDOWN = 270f;
        const float ECHOMODE_BUOYANCY_BONUS = 8f;
        const float ECHOMODE_WATERFRIC_BONUS = 1.1f;

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
        
        public static readonly Dictionary<int, MartyrFields> fieldsByPlayerHash = new Dictionary<int, MartyrFields>();
        private readonly static List<Hook> manualHooks = new List<Hook>();

        public static void Enable()
        {
            //lc
            On.RainWorldGame.ctor += GameStarts;
            On.Player.ctor += RegisterFieldset;
            On.Player.Update += RunAbilityCycle;

            //em
            On.Player.ThrownSpear += EchomodeDamageBonus;
            On.Player.MovementUpdate += EchomodeExtendRoll;
            On.Creature.SpearStick += EchomodeDeflection;
            On.Weapon.Thrown += EchomodeVelBonus;
            On.Creature.Violence += EchomodePreventDamage;

            //id
            On.PlayerGraphics.InitiateSprites += Player_MakeSprites;
            On.PlayerGraphics.AddToContainer += Player_ATC;
            On.PlayerGraphics.DrawSprites += Player_Draw;

            //misc
            On.Player.ctor += PromptCycleWarning;
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
            var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
            bubble.SetPosition(npos - camPos);
            bubble.alpha = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.element = Futile.atlasManager.GetElementWithName("Futile_White");
            var cf = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.scale = Lerp(13f, 16f, cf);
            bubble.isVisible = UnityEngine.Random.value < cf;
            var ccol = Color.Lerp(mf.lastBCol, mf.bCol, timeStacker);
            for (int i = 0; i < 9; i++) sLeaser.sprites[i].color = ccol;
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
        #endregion
        #region ability
        private static void EchomodeExtendRoll(On.Player.orig_MovementUpdate orig, 
            Player self, bool eu)
        {
            orig(self, eu);
            if (!fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf)) return;
            //arbitrary threshold but works so far
            if (mf.echoActive && self.rollCounter > 30) self.rollCounter = 30;
            //if (mf.echoActive) self.stopRollingCounter = 0;
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
            if (thrownBy is Player m && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) foreach (var c in self.bodyChunks) c.vel *= ECHOMODE_THROWFORCE_BONUS;
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
        
        private static void EchomodeDamageBonus(On.Player.orig_ThrownSpear orig, 
            Player self, Spear spear)
        {
            orig(self, spear);
            if (fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf))
            {
                if (mf.echoActive)
                {
                    spear.spearDamageBonus *= ECHOMODE_DAMAGE_BONUS;
                }
            }
        }
        #endregion
        #region lifecycle
        private static void RunAbilityCycle(On.Player.orig_Update orig, 
            Player self, bool eu)
        {
            if (!fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf)) return;
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
        }
        #endregion

        public static void Disable()
        {
            On.RainWorldGame.ctor -= GameStarts;
            On.Player.ctor -= RegisterFieldset;
            On.Player.Update -= RunAbilityCycle;

            On.Player.ThrownSpear -= EchomodeDamageBonus;
            On.Creature.SpearStick -= EchomodeDeflection;
            On.Weapon.Thrown -= EchomodeVelBonus;
            On.Creature.Violence -= EchomodePreventDamage;
            On.Player.MovementUpdate -= EchomodeExtendRoll;

            On.PlayerGraphics.InitiateSprites -= Player_MakeSprites;
            On.PlayerGraphics.AddToContainer -= Player_ATC;
            On.PlayerGraphics.DrawSprites -= Player_Draw;

            //On.RedsIllness.RedsCycles -= ChangeLimit;
            //On.HUD.Map.CycleLabel.UpdateCycleText -= ChangeMapCycleText;
            //On.HUD.SubregionTracker.Update -= SubregionTrackerText;

            On.Player.ctor -= PromptCycleWarning;

            foreach (var h in manualHooks) { h.Undo(); }
            manualHooks.Clear(); 
            
        }
    }

    internal class CyclePrompt : UpdatableAndDeletable
    {
        public override void Update(bool eu)
        {
            base.Update(eu);
            string message = $"Remaining cycles: {RemnantConfig.martyrCycles.Value - room.game?.rainWorld.progression.currentSaveState.cycleNumber}";
            room.game?.cameras[0].hud.textPrompt.AddMessage(message, 15, 400, false, false);
            Destroy();
        }
    }
}
