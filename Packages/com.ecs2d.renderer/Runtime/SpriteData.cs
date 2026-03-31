using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    public struct SpriteData : IComponentData
    {
        public float4 TranslationAndRotation;
        public float Scale;
        public float4 Color;
        public int SpriteFrameIndex;
        public int SpriteSheetId;
        public byte FlipX;
        public byte FlipY;
    }
}
