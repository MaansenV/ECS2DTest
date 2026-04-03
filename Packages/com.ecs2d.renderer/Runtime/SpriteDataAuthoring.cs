using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class SpriteDataAuthoring : MonoBehaviour
    {
        public SpriteSheetDefinition SpriteSheet;
        public int SortingLayer;
        public int SpriteFrameIndex;
        public float BaseScale = 1f;
        public float4 Color = new float4(1.0f, 1.0f, 1.0f, 1.0f);
        public float RotationOffsetDegrees;
        public bool FlipX;
        public bool FlipY;

        private class SpriteDataAuthoringBaker : Baker<SpriteDataAuthoring>
        {
            public override void Bake(SpriteDataAuthoring authoring)
            {
                if (authoring.SpriteSheet == null)
                {
                    Debug.LogError($"{nameof(SpriteDataAuthoring)} on '{authoring.name}' is missing a SpriteSheet reference.");
                    return;
                }

                DependsOn(authoring.transform);
                DependsOn(authoring.SpriteSheet);

                Vector3 lossyScale = authoring.transform.lossyScale;
                if (math.abs(lossyScale.x - lossyScale.y) > 0.0001f)
                {
                    Debug.LogWarning($"{nameof(SpriteDataAuthoring)} on '{authoring.name}' uses non-uniform scale. The renderer will use X scale for a uniform sprite size.");
                }

                bool flipX = authoring.FlipX ^ (lossyScale.x < 0f);
                bool flipY = authoring.FlipY ^ (lossyScale.y < 0f);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                int frameCount = math.max(1, authoring.SpriteSheet.FrameCount);
                int spriteFrameIndex = math.clamp(authoring.SpriteFrameIndex, 0, frameCount - 1);
                float rotationOffsetRadians = math.radians(authoring.RotationOffsetDegrees);
                float rotationRadians = math.radians(authoring.transform.eulerAngles.z) + rotationOffsetRadians;
                float scale = authoring.BaseScale * math.abs(lossyScale.x);
                Vector3 position = authoring.transform.position;

                var data = new SpriteData
                {
                    TranslationAndRotation = new float4(position.x, position.y, position.z, rotationRadians),
                    BaseScale = authoring.BaseScale,
                    RotationOffsetRadians = rotationOffsetRadians,
                    Scale = scale,
                    Color = authoring.Color,
                    RenderDepth = SpriteSortingUtility.CalculateRenderDepth(authoring.SortingLayer, position.y, authoring.SpriteSheet.SheetId, position.z),
                    SpriteFrameIndex = spriteFrameIndex,
                    SpriteSheetId = authoring.SpriteSheet.SheetId,
                    SortingLayer = authoring.SortingLayer,
                    FlipX = (byte)(flipX ? 1 : 0),
                    FlipY = (byte)(flipY ? 1 : 0)
                };

                AddComponent(entity, data);
                AddComponent<SpriteCullState>(entity);
                AddSharedComponent(entity, SpriteSheetRuntime.CreateRenderKey(authoring.SpriteSheet.SheetId));
            }
        }
    }
}
