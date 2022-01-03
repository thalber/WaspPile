using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Reflection;

using static RWCustom.Custom;
using static UnityEngine.Mathf;
using static WaspPile.Remnant.Satellite.RemnantUtils;
using static Mono.Cecil.Cil.OpCodes;
using static UnityEngine.Debug;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant
{
    public static partial class CommonHooks
    {
        internal static readonly List<IDetour> manualHooks = new();
        internal static void Enable()
        {
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavAI_PearlCost;

            //marty pearl
            On.DataPearl.InitiateSprites += Pearl_MakeSprites;
            On.DataPearl.DrawSprites += Pearl_Draw;
            On.DataPearl.AddToContainer += Pearl_ATC;
            //On.RainWorldGame.Update += ApplyHitFrames;
            if (RemnantPlugin.DebugMode)
            {
                On.Creature.Violence += LogDamage;
            }
            Satellite.ArenaIcons.Apply();
        }

        private static void LogDamage(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            orig(self,
                source,
                directionAndMomentum,
                hitChunk,
                hitAppendage,
                type,
                damage,
                stunBonus);
            LogWarning(
                $"CREATURE {self.Template.type}, HIT BY {source?.owner.abstractPhysicalObject?.type} FOR {(damage, stunBonus)}, REMAINING HEALTH: {(self.State is HealthState hs ? hs.health : "N/A")}, ALIVE: {self.State.alive}");
        }

        internal static int freeze = 0;
        private static void ApplyHitFrames(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            if (freeze > 0) { freeze--; return; }
            orig(self);
        }

        #region martyr pearl
        private static int ScavAI_PearlCost(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            //make sure they don't steal
            if (obj is DataPearl p && IsEchoPearl(p)) return 0;
            return orig(self, obj, weaponFiltered);
        }

        internal static bool IsEchoPearl(this DataPearl instance) => instance.AbstractPearl.IsEchoPearl();
        internal static bool IsEchoPearl(this DataPearl.AbstractDataPearl instance) => instance.dataPearlType.ToString().Contains("MARTYR_MESSAGE");
        private static bool PEARL_SIN_LOCK;
        private static void Pearl_MakeSprites(On.DataPearl.orig_InitiateSprites orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            PEARL_SIN_LOCK = true;
            orig(self, sLeaser, rCam);
            PEARL_SIN_LOCK = false;
            if (IsEchoPearl(self))
            {
                foreach (var s in sLeaser.sprites) s.RemoveFromContainer();
                Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 1);
                var halo = new FSprite("Futile_White")
                {
                    shader = self.room.game.rainWorld.Shaders["GhostDistortion"],
                    scale = 3f
                };
                sLeaser.sprites[sLeaser.sprites.Length - 1] = halo;
                self.AddToContainer(sLeaser, rCam, null);
            }
        }
        private static void Pearl_ATC(On.DataPearl.orig_AddToContainer orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (PEARL_SIN_LOCK) return;
            if (IsEchoPearl(self))
            {
                var halo = sLeaser.sprites.Last();
                halo.RemoveFromContainer();
                rCam.ReturnFContainer("ForegroundLights").AddChild(halo);
            }
        }
        private static void Pearl_Draw(On.DataPearl.orig_DrawSprites orig, DataPearl self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsEchoPearl(self))
            {
                var halo = sLeaser.sprites.Last();
                var body = sLeaser.sprites[0];
                halo.SetPosition(body.GetPosition());
            }
        }
        #endregion

        internal static void Disable()
        {
            On.ScavengerAI.CollectScore_PhysicalObject_bool -= ScavAI_PearlCost;

            On.DataPearl.InitiateSprites -= Pearl_MakeSprites;
            On.DataPearl.DrawSprites -= Pearl_Draw;
            On.DataPearl.AddToContainer -= Pearl_ATC;

            On.RainWorldGame.Update -= ApplyHitFrames;
            On.Creature.Violence -= LogDamage;
            foreach (var hk in manualHooks) hk.Undo();
            manualHooks.Clear();
            Satellite.ArenaIcons.Undo();
        }
    }
}
