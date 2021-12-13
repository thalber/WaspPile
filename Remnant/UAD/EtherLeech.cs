using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using URand = UnityEngine.Random;

namespace WaspPile.Remnant.UAD
{
    public class EtherLeech : UpdatableAndDeletable, IDrawable
    {
        public EtherLeech(PhysicalObject po)
        {

        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            
        }
        internal PhysicalObject owner;
        internal attachPosData pos;

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            throw new NotImplementedException();
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            throw new NotImplementedException();
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            throw new NotImplementedException();
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite(Futile.atlasManager.GetElementWithName("JaggedCircle"));
            sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(URand.Range(5, 10), true, true);
        }

        internal struct attachPosData
        {
            internal int chunk0;
            internal int? chunk1;
            internal float k;
            internal float sOffs;
            internal float relrot;
            internal static attachPosData initfor(PhysicalObject po)
            {
                throw new NotImplementedException();
            }
        }
    }
}
