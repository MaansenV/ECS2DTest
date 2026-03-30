using Unity.Collections;
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

        public FixedString64Bytes CurrentAnimation;
        public FixedString64Bytes LastResolvedAnimation;
        public float Time;
        public float PlaybackSpeed;
        public bool Playing;
        public int CurrentFrameIndex;
        public int CurrentClipIndex;
        public byte Flags;
    }
}
