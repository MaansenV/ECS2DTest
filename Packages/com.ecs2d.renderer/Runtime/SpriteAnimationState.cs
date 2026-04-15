using Unity.Entities;

namespace ECS2D.Rendering
{
    public struct SpriteAnimationSetReference : IComponentData
    {
        public BlobAssetReference<SpriteAnimationSetBlob> Value;
    }

    public struct SpriteAnimationState : IComponentData
    {
        public const byte InitializedFlag = 1;

        public float Time;
        public float PlaybackSpeed;
        public int CurrentClipIndex;
        public int CurrentFrameIndex;
        public byte Flags;
        public byte Playing;
    }

    public struct SpriteAnimationChangeRequest : IComponentData
    {
        public int ClipIndex;
        public float StartTime;
        public byte Restart;
    }
}
