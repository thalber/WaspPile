using UnityEngine;

namespace THP
{

    public static class EnumExt_THP
    {
        public static AbstractPhysicalObject.AbstractObjectType Jumpoline;
    }

    public class AbstractJumpoline : AbstractPhysicalObject
    {
        public AbstractJumpoline(World world, Jumpoline realizedObject, WorldCoordinate pos, EntityID ID) : base (world, EnumExt_THP.Jumpoline, realizedObject, pos, ID)
        {
            
        }
    }
    public class Jumpoline : PhysicalObject, IDrawable
    {
        public Jumpoline(AbstractJumpoline abstractObject) : base(abstractObject)
        {
            this.bodyChunks = new BodyChunk[1];
            this.bodyChunks[0] = new BodyChunk(this, 0, new UnityEngine.Vector2(0f, 0f), 40f, 10f);
            this.bodyChunkConnections = new BodyChunkConnection[0];
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.4f;
            this.collisionLayer = 2;
            base.waterFriction = 0.98f;
            base.buoyancy = 0.4f;
            //this.pivotAtTip = false;
            //this.lastPivotAtTip = false;
            //this.stuckBodyPart = -1;
            base.firstChunk.loudness = 7f;
            //this.tailPos = base.firstChunk.pos;
            //this.soundLoop = new ChunkDynamicSoundLoop(base.firstChunk);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Pixel");
            sLeaser.sprites[0].scale = 10f;
        }
        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {

        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {

        }
    }
}
