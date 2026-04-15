using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class SpriteDataAuthoring : MonoBehaviour
    {
        [Tooltip("Sprite sheet asset that defines the texture, frame layout, and render key for this sprite.")]
        public SpriteSheetDefinition SpriteSheet;

        [Tooltip("Rendering layer used to order this sprite against other sprites.")]
        public int SortingLayer;

        [Tooltip("Initial sprite frame index used before animation overrides it.")]
        [Range(0, 100)]
        public int SpriteFrameIndex;

        [Tooltip("Per-sprite size multiplier applied on top of the GameObject scale.")]
        [Range(0.01f, 100f)]
        public float BaseScale = 1f;

        [Tooltip("Optional non-uniform sprite size multiplier. When set, this overrides BaseScale and applies separate X/Y scaling.")]
        public Vector2 BaseScaleXY;

        [SerializeField, HideInInspector]
        private bool useAdvancedScaleXY;

        [Tooltip("Vertex color tint applied when rendering this sprite.")]
        public float4 Color = new float4(1.0f, 1.0f, 1.0f, 1.0f);

        [Tooltip("Additional local rotation offset applied in degrees before rendering.")]
        [Range(-360f, 360f)]
        public float RotationOffsetDegrees;

        [Tooltip("Mirror the sprite horizontally before transform-based flip handling.")]
        public bool FlipX;

        [Tooltip("Mirror the sprite vertically before transform-based flip handling.")]
        public bool FlipY;

        private class SpriteDataAuthoringBaker : Baker<SpriteDataAuthoring>
        {
            private static float2 ResolveBaseScaleXY(SpriteDataAuthoring authoring)
            {
                if (!authoring.useAdvancedScaleXY)
                {
                    return new float2(authoring.BaseScale, authoring.BaseScale);
                }

                Vector2 explicitScaleXY = authoring.BaseScaleXY;
                return math.max(float2.zero, new float2(explicitScaleXY.x, explicitScaleXY.y));
            }

            private static bool TryResolveAnimationStartFrame(SpriteAnimationAuthoring animationAuthoring, out int spriteFrameIndex)
            {
                spriteFrameIndex = 0;

                if (animationAuthoring == null || animationAuthoring.AnimationSet == null || animationAuthoring.AnimationSet.SpriteSheet == null)
                {
                    return false;
                }

                SpriteSheetDefinition spriteSheet = animationAuthoring.AnimationSet.SpriteSheet;
                if (!spriteSheet.AutoGenerateGridFrames)
                {
                    return false;
                }

                var clips = animationAuthoring.AnimationSet.Clips;
                if (clips == null || clips.Count == 0)
                {
                    return false;
                }

                int columns = math.max(1, spriteSheet.Columns);
                int rows = math.max(1, spriteSheet.Rows);
                int frameCount = math.max(1, spriteSheet.FrameCount);
                int fallbackIndex = -1;

                for (int i = 0; i < clips.Count; i++)
                {
                    SpriteAnimationClip clip = clips[i];
                    if (string.IsNullOrWhiteSpace(clip.Name) || clip.FrameCount <= 0)
                    {
                        continue;
                    }

                    if (clip.Row < 0 || clip.Row >= rows || clip.StartColumn < 0 || clip.StartColumn >= columns)
                    {
                        continue;
                    }

                    int endColumn = clip.StartColumn + clip.FrameCount - 1;
                    if (endColumn >= columns)
                    {
                        continue;
                    }

                    int resolvedFrameIndex = (clip.Row * columns) + clip.StartColumn;
                    if (resolvedFrameIndex < 0 || resolvedFrameIndex + clip.FrameCount > frameCount)
                    {
                        continue;
                    }

                    if (fallbackIndex < 0)
                    {
                        fallbackIndex = resolvedFrameIndex;
                    }

                    if (!string.IsNullOrWhiteSpace(animationAuthoring.StartAnimation)
                        && string.Equals(clip.Name, animationAuthoring.StartAnimation, System.StringComparison.Ordinal))
                    {
                        spriteFrameIndex = resolvedFrameIndex;
                        return true;
                    }
                }

                if (fallbackIndex < 0)
                {
                    return false;
                }

                spriteFrameIndex = fallbackIndex;
                return true;
            }

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
                float2 baseScaleXY = ResolveBaseScaleXY(authoring);

                bool flipX = authoring.FlipX ^ (lossyScale.x < 0f);
                bool flipY = authoring.FlipY ^ (lossyScale.y < 0f);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                int frameCount = math.max(1, authoring.SpriteSheet.FrameCount);
                int spriteFrameIndex = math.clamp(authoring.SpriteFrameIndex, 0, frameCount - 1);
                var animationAuthoring = GetComponent<SpriteAnimationAuthoring>();
                if (TryResolveAnimationStartFrame(animationAuthoring, out int resolvedStartFrameIndex))
                {
                    spriteFrameIndex = math.clamp(resolvedStartFrameIndex, 0, frameCount - 1);
                }

                float rotationOffsetRadians = math.radians(authoring.RotationOffsetDegrees);
                float rotationRadians = math.radians(authoring.transform.eulerAngles.z) + rotationOffsetRadians;
                float2 scaleXY = baseScaleXY * math.abs(new float2(lossyScale.x, lossyScale.y));
                Vector3 position = authoring.transform.position;

                var data = new SpriteData
                {
                    TranslationAndRotation = new float4(position.x, position.y, position.z, rotationRadians),
                    BaseScale = authoring.BaseScale,
                    BaseScaleXY = baseScaleXY,
                    UseAdvancedScaleXY = (byte)(authoring.useAdvancedScaleXY ? 1 : 0),
                    RotationOffsetRadians = rotationOffsetRadians,
                    Scale = scaleXY.x,
                    ScaleXY = scaleXY,
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
