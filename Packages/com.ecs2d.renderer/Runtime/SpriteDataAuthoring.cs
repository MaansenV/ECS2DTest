using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class SpriteDataAuthoring : MonoBehaviour
    {
        public SpriteSheetDefinition SpriteSheet;
        public int SpriteFrameIndex;
        public float4 TranslationAndRotation;
        public float Scale = 0.3f;
        public float4 Color = new float4(1.0f, 1.0f, 1.0f, 1.0f);

        class Baker : Baker<SpriteDataAuthoring>
        {
            public override void Bake(SpriteDataAuthoring authoring)
            {
                if (authoring.SpriteSheet == null)
                {
                    Debug.LogError($"{nameof(SpriteDataAuthoring)} on '{authoring.name}' is missing a SpriteSheet reference.");
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.None);
                int frameCount = math.max(1, authoring.SpriteSheet.FrameCount);
                int spriteFrameIndex = math.clamp(authoring.SpriteFrameIndex, 0, frameCount - 1);

                var data = new SpriteData
                {
                    TranslationAndRotation = authoring.TranslationAndRotation,
                    Scale = authoring.Scale,
                    Color = authoring.Color,
                    SpriteFrameIndex = spriteFrameIndex,
                    SpriteSheetId = authoring.SpriteSheet.SheetId
                };

                AddComponent(entity, data);
            }
        }
    }
}
