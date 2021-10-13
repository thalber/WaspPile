using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaspPile.Remnant
{
    public static class CommonHooks
    {
        public static void Enable()
        {
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavAI_PearlCost;

            //marty pearl
            On.DataPearl.InitiateSprites += Pearl_MakeSprites;
            On.DataPearl.DrawSprites += Pearl_Draw;
            On.DataPearl.AddToContainer += Pearl_ATC;
        }

        private static int ScavAI_PearlCost(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            //make sure they don't steal
            if (obj is DataPearl p && IsEchoPearl(p)) return 0;
            return orig(self, obj, weaponFiltered);
        }

        #region martyr pearl

        private static bool IsEchoPearl(DataPearl instance)
        {
#warning add better pearltype detection
            return instance.AbstractPearl.dataPearlType.ToString().Contains("MARTYR");
        }
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
                var halo = new FSprite("Futile_White");
                halo.shader = self.room.game.rainWorld.Shaders["GhostDistortion"];
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

        public static void Disable()
        {
            On.ScavengerAI.CollectScore_PhysicalObject_bool -= ScavAI_PearlCost;

            On.DataPearl.InitiateSprites -= Pearl_MakeSprites;
            On.DataPearl.DrawSprites -= Pearl_Draw;
            On.DataPearl.AddToContainer -= Pearl_ATC;
        }
    }
}
