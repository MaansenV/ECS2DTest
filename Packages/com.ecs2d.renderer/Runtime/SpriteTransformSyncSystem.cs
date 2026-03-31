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
            private void Execute(ref SpriteData spriteData, in LocalToWorld localToWorld)
            {
                float4x4 matrix = localToWorld.Value;
                float3 xAxis = matrix.c0.xyz;
                float3 yAxis = matrix.c1.xyz;
                float3 position = localToWorld.Position;

                float scaleX = math.length(xAxis);
                float det2D = xAxis.x * yAxis.y - xAxis.y * yAxis.x;

                float rotationRadians;
                byte flipX = 0;

                if (scaleX > 0.0001f)
                {
                    rotationRadians = math.atan2(xAxis.y, xAxis.x);

                    if (math.abs(rotationRadians + math.PI) < 0.0001f)
                    {
                        rotationRadians = math.PI;
                    }

                    if (det2D < 0f)
                    {
                        float scaleY = math.length(yAxis);
                        if (scaleY > 0.0001f)
                        {
                            rotationRadians = math.atan2(-yAxis.x, yAxis.y);

                            if (math.abs(rotationRadians + math.PI) < 0.0001f)
                            {
                                rotationRadians = math.PI;
                            }
                        }
                        flipX = 1;
                    }
                }
                else
                {
                    float scaleY = math.length(yAxis);
                    rotationRadians = scaleY > 0.0001f
                        ? math.atan2(-yAxis.x, yAxis.y)
                        : 0f;

                    if (math.abs(rotationRadians + math.PI) < 0.0001f)
                    {
                        rotationRadians = math.PI;
                    }
                }

                spriteData.TranslationAndRotation = new float4(position, rotationRadians);
                spriteData.Scale = scaleX;
                spriteData.FlipX = flipX;
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
