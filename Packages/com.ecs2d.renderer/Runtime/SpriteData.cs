using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    public struct SpriteData : IComponentData
    {
        public float4 TranslationAndRotation;
        public float BaseScale;
        public float2 BaseScaleXY;
        public byte UseAdvancedScaleXY;
        public float RotationOffsetRadians;
        public float Scale;
        public float2 ScaleXY;
        public float4 Color;
        public float RenderDepth;
        public int SpriteFrameIndex;
        public int SpriteSheetId;
        public int SortingLayer;
        public byte FlipX;
        public byte FlipY;
    }
}
