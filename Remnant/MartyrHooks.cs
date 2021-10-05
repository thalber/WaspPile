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
//?visuals
//+throwforce and damage
//-unhooking
//+waterbounce(crude)
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
        const KeyCode ECHOMODE_TRIGGERKEY = KeyCode.Tab;

        public struct MartyrFields
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
            mf.cooldown = fullDeplete ? 
                ECHOMODE_DEPLETE_COOLDOWN 
                : ECHOMODE_DEPLETE_COOLDOWN * InverseLerp(mf.maxEchoReserve, 0f, mf.echoReserve) * 0.8f;
        }
        public static void powerUp(this Player self, ref MartyrFields mf)
        {
            if (mf.cooldown > 0) return;
        }
        public static readonly Dictionary<int, MartyrFields> fieldsByHashcode = new Dictionary<int, MartyrFields>();

        public static void Enable()
        {
            On.RainWorldGame.ctor += GameStarts;
            //On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.Player.ctor += RegisterFieldset;
            On.Player.ThrownSpear += EchomodeDamageBonus;
            On.Player.MovementUpdate += RunAbilityCycle;
            On.Creature.SpearStick += EchomodeDeflection;
            On.Weapon.Thrown += EchomodeVelBonus;
            On.PlayerGraphics.InitiateSprites += Player_SpriteInit;
            //On.PlayerGraphics.AddToContainer += Player_AddToContainer;
            On.PlayerGraphics.DrawSprites += Player_DrawSprites;
        }

        private static void Player_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!fieldsByHashcode.TryGetValue(self.GetHashCode(), out var mf)) return;
            var npos = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            var bubble = sLeaser.sprites[mf.bubbleSpriteIndex];
            bubble.SetPosition(npos - camPos);
            bubble.alpha = Lerp(mf.lastFade, mf.fade, timeStacker);
            bubble.scale = 16f;
            bubble.isVisible = mf.echoActive;
        }

        private static void Player_SpriteInit(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (!fieldsByHashcode.TryGetValue(self.GetHashCode(), out var mf)) return;
            foreach (var sprite in sLeaser.sprites) sprite.RemoveFromContainer();
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
            mf.bubbleSpriteIndex = sLeaser.sprites.Length - 1;
            sLeaser.sprites[mf.bubbleSpriteIndex] = new FSprite(Futile.atlasManager.GetElementWithName("Futile_White"));
            sLeaser.sprites[mf.bubbleSpriteIndex].shader = self.player.room.game.rainWorld.Shaders["GhostDistortion"];
            self.AddToContainer(sLeaser, rCam, null);
        }

        private static void EchomodeVelBonus(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (thrownBy is Player m && fieldsByHashcode.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) foreach (var c in self.bodyChunks) c.vel *= ECHOMODE_THROWFORCE_BONUS;
            }
        }
        private static bool EchomodeDeflection(On.Creature.orig_SpearStick orig, Creature self, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 direction)
        {
            if (self is Player m && fieldsByHashcode.TryGetValue(m.GetHashCode(), out var mf))
            {
                if (mf.echoActive) return false;
            }
            return orig(self, source, dmg, chunk, appPos, direction);
        }
        private static void RunAbilityCycle(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (!fieldsByHashcode.TryGetValue(self.GetHashCode(), out var mf)) return;
            //basic recharge/cooldown and activation
            bool toggleRequested = Input.GetKeyDown(ECHOMODE_TRIGGERKEY);
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
                mf.echoReserve += mf.rechargeRate;
                mf.cooldown = Max(0, mf.cooldown - 1f);
                if (toggleRequested) self.powerUp(ref mf);
            }
            //basic stats modification 
            self.slugcatStats.runspeedFac = mf.echoActive ? mf.baseRunSpeed * ECHOMODE_RUNSPEED_BONUS : mf.baseRunSpeed;
            self.buoyancy = mf.echoActive ? mf.baseBuoyancy * ECHOMODE_BUOYANCY_BONUS : mf.baseBuoyancy;
            self.waterFriction = mf.echoActive ? mf.baseWaterFric * ECHOMODE_WATERFRIC_BONUS : mf.baseWaterFric;
            mf.lastFade = mf.fade;
            mf.fade = Custom.LerpAndTick(mf.fade, mf.echoActive ? 0.8f : 0f, 0.2f, 0.05f);
            orig(self, eu);
        }
        private static void EchomodeDamageBonus(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig(self, spear);
            if (fieldsByHashcode.TryGetValue(self.GetHashCode(), out var mf))
            {
                if (mf.echoActive)
                {
                    spear.spearDamageBonus *= ECHOMODE_DAMAGE_BONUS;
                }
            }
        }
        private static void RegisterFieldset(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            fieldsByHashcode.Add(self.GetHashCode(), new MartyrFields()
            {
                maxEchoReserve = 520f,
                rechargeRate = 0.5f,
                baseBuoyancy = self.buoyancy,
                baseRunSpeed = self.slugcatStats.runspeedFac,
                baseWaterFric = self.waterFriction
            }) ;
        }
        private static void GameStarts(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig(self, manager);
            fieldsByHashcode.Clear();
        }

        public static void Disable()
        {
#error add unhooks after hooks are finalized
        }
    }
}
