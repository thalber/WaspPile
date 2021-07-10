using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WaspPile.ElectroSosig
{
    public class ESMod : Partiality.Modloader.PartialityMod
    {
        public ESMod()
        {
            this.author = "thalber";
            this.ModID = "ElecSosigs";
            this.Version = "0";
        }
        public override void OnEnable()
        {
            base.OnEnable();
            On.CentipedeGraphics.DrawSprites += CentiDrawSprites;
        }

        public static void CentiDrawSprites
                (On.CentipedeGraphics.orig_DrawSprites orig, 
                CentipedeGraphics instance, 
                RoomCamera.SpriteLeaser sleaser, 
                RoomCamera rcam, 
                float ts,
                Vector2 cpos)
        {
            orig(instance, sleaser, rcam, ts, cpos);
            for (int i = 0; i < instance.centipede.bodyChunks.Length; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    sleaser.sprites[instance.LegSprite(i, j, 0)].alpha = 0f;
                    sleaser.sprites[instance.LegSprite(i, j, 1)].alpha = 0f;
                }
            }
            for (int k = 0; k < 2; k++)
            {
                for (int l = 0; l < 2; l++)
                {
                    for (int m = 0; m < 2; m++)
                    {
                        sleaser.sprites[instance.WhiskerSprite(k, l, m)].alpha = 0;
                    }
                }
            }
        }

    }
}
