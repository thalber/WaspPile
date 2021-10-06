using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using RWCustom;
using UnityEngine;
using static UnityEngine.Mathf;

//todo
//+base ability lifecycle
//?visuals (bubble container problems)
//+throwforce and damage
//+deflect
//+unhooking
//?waterbounce(crude but origo seems same) ((actually pretty bad and should be done properly))
//-ac/deac sounds
//-soft fade? does the shader even support it lmao
//-em: infinite rolls
//-import art
//-testing

namespace WaspPile.Remnant
{
    public static class MartyrHooks 
    {
#warning stats are pretty arbitrary
        const float ECHOMODE_DAMAGE_BONUS = 1.5f;
        const float ECHOMODE_THROWFORCE_BONUS = 1.8f;
        const float ECHOMODE_RUNSPEED_BONUS = 1.15f;
        const float ECHOMODE_DEPLETE_COOLDOWN = 270f;
        const float ECHOMODE_BUOYANCY_BONUS = 8f;
        const float ECHOMODE_WATERFRIC_BONUS = 2f;
        const KeyCode ECHOMODE_TRIGGERKEY = KeyCode.LeftControl;

        //represents additional martyr related fields for Player
        public class MartyrFields
        {
            //idk
            public float submergedTime;
            //ability
            public float maxEchoReserve;
            public float rechargeRate;
            public float echoReserve;
            //cooldown after depletion turning off, ticks down by 1f every frame
            public float cooldown;
            public bool echoActive;

            public float lastFade;
            public float fade; //using for fade in-out
            //public bool fullStomach;
            public float buttonPressed; //?
            public float bonusEchoTime; //?
            public float defEchoTime; //?
            //service
            public int bubbleSpriteIndex;
            public float baseBuoyancy;
            public float baseRunSpeed;
            public float baseWaterFric;
        }
        public static void powerDown(this Player self, ref MartyrFields mf, bool fullDeplete = false)
        {
            Console.WriteLine("Martur ability down");
            mf.cooldown = fullDeplete ? 
                ECHOMODE_DEPLETE_COOLDOWN 
                : ECHOMODE_DEPLETE_COOLDOWN * InverseLerp(mf.maxEchoReserve, 0f, mf.echoReserve) * 0.8f;
            Console.WriteLine($"cd: {mf.cooldown}");
            mf.echoActive = false;
        }
        public static void powerUp(this Player self, ref MartyrFields mf)
        {
            Console.WriteLine($"Martur ability up\nreserve: {mf.echoReserve}");
            mf.echoActive = true;
        }
        public static readonly Dictionary<int, MartyrFields> fieldsByPlayerHash = new Dictionary<int, MartyrFields>();

        public static void Enable()
        {
            On.RainWorldGame.ctor += GameStarts;
            //On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.Player.ctor += RegisterFieldset;
            On.Player.ThrownSpear += EchomodeDamageBonus;
            On.Player.Update += RunAbilityCycle;
            On.Creature.SpearStick += EchomodeDeflection;
            On.Weapon.Thrown += EchomodeVelBonus;
            On.PlayerGraphics.InitiateSprites += Player_SpriteInit;
            On.PlayerGraphics.AddToContainer += Player_AddToContainer;
            On.PlayerGraphics.DrawSprites += Player_DrawSprites;
            On.Creature.Violence += EchomodePreventDamage;
        }

        //IDrawable
        private static void Player_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (!fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf) || mf.bubbleSpriteIndex == -1 || VANILLA_SIN_LOCK) return;
            try
            {
                Console.WriteLine($"martyr addtocont: bubble indes {mf.bubbleSpriteIndex} sleaser.s length {sLeaser.sprites.Length}");
                var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
                bubble.RemoveFromContainer();
                rCam.ReturnFContainer("Bloom").AddChild(bubble);
            }
            catch (IndexOutOfRangeException) { Console.WriteLine("Something went bad on martyr sprite"); }
        }
        private static void Player_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.owner.slatedForDeletetion || self.owner.room != rCam.room || !fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf)) return;
            var npos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
            bubble.SetPosition(npos - camPos);
            bubble.alpha = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.scale = 16f;
            bubble.element = Futile.atlasManager.GetElementWithName("Futile_White");
            bubble.isVisible = mf.echoActive;
        }
        private static bool VANILLA_SIN_LOCK;
        private static void Player_SpriteInit(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            VANILLA_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            VANILLA_SIN_LOCK = false;
            if (!fieldsByPlayerHash.TryGetValue(self.player.GetHashCode(), out var mf)) return;
            foreach (var sprite in sLeaser.sprites) sprite.RemoveFromContainer();
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
            mf.bubbleSpriteIndex = sLeaser.sprites.Length - 1;
            Console.WriteLine($"martyr bubble sprite: {mf.bubbleSpriteIndex}");
            sLeaser.sprites[mf.bubbleSpriteIndex] = new FSprite(Futile.atlasManager.GetElementWithName("Futile_White"));
            sLeaser.sprites[mf.bubbleSpriteIndex].shader = self.player.room.game.rainWorld.Shaders["GhostDistortion"];
            self.AddToContainer(sLeaser, rCam, null);
        }
        //various ability aspects
        private static void EchomodePreventDamage(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (self is Player m
                && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf)
                && mf.echoActive
                && source?.owner is Spear) damage = 0f; 
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void EchomodeVelBonus(On.Weapon.orig_Thrown orig, 
            Weapon self, 
            Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (thrownBy is Player m && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) foreach (var c in self.bodyChunks) c.vel *= ECHOMODE_THROWFORCE_BONUS;
            }
        }
        private static bool EchomodeDeflection(On.Creature.orig_SpearStick orig, Creature self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            if (self is Player m && fieldsByPlayerHash.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) return false;
            }
            return orig(self, source, dmg, chunk, appPos, direction);
        }
        private static void EchomodeDamageBonus(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
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
        //lifecycle and initialization
        private static void RunAbilityCycle(On.Player.orig_Update orig, Player self, bool eu)
        {
            if (!fieldsByPlayerHash.TryGetValue(self.GetHashCode(), out var mf)) return;
            //basic recharge/cooldown and activation
            bool toggleRequested = Input.GetKeyDown(ECHOMODE_TRIGGERKEY);
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
            }

            //basic stats modification
            self.slugcatStats.runspeedFac = mf.echoActive ? mf.baseRunSpeed * ECHOMODE_RUNSPEED_BONUS : mf.baseRunSpeed;
            self.buoyancy = mf.echoActive ? mf.baseBuoyancy * ECHOMODE_BUOYANCY_BONUS : mf.baseBuoyancy;
            self.waterFriction = mf.echoActive ? mf.baseWaterFric * ECHOMODE_WATERFRIC_BONUS : mf.baseWaterFric;
            mf.lastFade = mf.fade;
            mf.fade = Custom.LerpAndTick(mf.fade, mf.echoActive ? 1f : 0f, 0.2f, 0.03f);
            orig(self, eu);
        }
        private static void RegisterFieldset(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
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
                bubbleSpriteIndex = -1
            }) ;
        }
        private static void GameStarts(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            fieldsByPlayerHash.Clear();
        }

        public static void Disable()
        {
            On.RainWorldGame.ctor -= GameStarts;
            On.Player.ctor -= RegisterFieldset;
            On.Player.ThrownSpear -= EchomodeDamageBonus;
            On.Player.Update -= RunAbilityCycle;
            On.Creature.SpearStick -= EchomodeDeflection;
            On.Weapon.Thrown -= EchomodeVelBonus;
            On.PlayerGraphics.InitiateSprites -= Player_SpriteInit;
            On.PlayerGraphics.AddToContainer -= Player_AddToContainer;
            On.PlayerGraphics.DrawSprites -= Player_DrawSprites;
            On.Creature.Violence -= EchomodePreventDamage;
        }
    }
}
