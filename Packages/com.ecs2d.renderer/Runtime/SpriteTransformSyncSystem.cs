using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteCullingSystem))]
    public partial struct SpriteTransformSyncSystem : ISystem
    {
        private EntityQuery spriteSyncQuery;

        [BurstCompile]
        private partial struct SyncSpriteTransformJob : IJobEntity
        {
            private static float NormalizeAngle(float angle)
            {
                if (math.abs(angle + math.PI) < 0.0001f)
                {
                    return math.PI;
                }

                return angle;
            }

            private void Execute(ref SpriteData spriteData, in LocalToWorld localToWorld)
            {
                float4x4 matrix = localToWorld.Value;
                float2 xAxis = matrix.c0.xy;
                float2 yAxis = matrix.c1.xy;
                float3 position = localToWorld.Position;

                float scaleX = math.length(xAxis);
                float determinant = (xAxis.x * yAxis.y) - (xAxis.y * yAxis.x);
                bool reflected = determinant < 0f;
                float2 deReflectedXAxis = reflected ? -xAxis : xAxis;

                float rotationRadians;

                if (scaleX > 0.0001f)
                {
                    rotationRadians = NormalizeAngle(math.atan2(deReflectedXAxis.y, deReflectedXAxis.x));
                }
                else
                {
                    float scaleY = math.length(yAxis);
                    float2 fallbackYAxis = reflected ? yAxis : -yAxis;
                    rotationRadians = scaleY > 0.0001f
                        ? NormalizeAngle(math.atan2(-fallbackYAxis.x, fallbackYAxis.y))
                        : 0f;
                }

                spriteData.TranslationAndRotation = new float4(position, rotationRadians + spriteData.RotationOffsetRadians);
                spriteData.Scale = scaleX * spriteData.BaseScale;
                spriteData.RenderDepth = SpriteSortingUtility.CalculateRenderDepth(spriteData.SortingLayer, position.y, spriteData.SpriteSheetId, position.z);
                spriteData.FlipX = (byte)(reflected ? 1 : 0);
                spriteData.FlipY = 0;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            spriteSyncQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpriteData, LocalToWorld>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(ref state);
            spriteSyncQuery.SetChangedVersionFilter(ComponentType.ReadOnly<LocalToWorld>());

            state.RequireForUpdate(spriteSyncQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SyncSpriteTransformJob().ScheduleParallel(spriteSyncQuery, state.Dependency);
        }
    }
}
