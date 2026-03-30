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
                float rotationRadians = math.atan2(matrix.c0.y, matrix.c0.x);
                float scale = math.length(matrix.c0.xyz);
                float3 position = localToWorld.Position;

                spriteData.TranslationAndRotation = new float4(position, rotationRadians);
                spriteData.Scale = scale;
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
