using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SpriteTransformSyncSystem))]
    [UpdateBefore(typeof(SpriteSystem))]
    public partial struct SpriteCullingSystem : ISystem
    {
        private const float EdgePadding = 0.05f;

        private EntityQuery spriteCullQuery;
        private EntityQuery movedSpriteCullQuery;
        private EntityQuery disabledSpriteCullQuery;
        private bool hasCameraSnapshot;
        private int cachedCameraInstanceId;
        private float2 cachedCameraPosition;
        private float cachedOrthographicSize;
        private float cachedAspect;

        [BurstCompile]
        private partial struct UpdateSpriteCullStateJob : IJobEntity
        {
            public float2 CameraMin;
            public float2 CameraMax;
            public float Padding;

            private void Execute(in LocalToWorld localToWorld, EnabledRefRW<SpriteCullState> cullState)
            {
                float3 position = localToWorld.Position;
                float scaleX = math.length(localToWorld.Value.c0.xyz);
                float scaleY = math.length(localToWorld.Value.c1.xyz);
                float halfExtent = (math.max(math.abs(scaleX), math.abs(scaleY)) * 0.5f) + Padding;
                float2 spritePosition = position.xy;
                float2 spriteMin = spritePosition - halfExtent;
                float2 spriteMax = spritePosition + halfExtent;

                bool isVisible =
                    spriteMax.x >= CameraMin.x &&
                    spriteMin.x <= CameraMax.x &&
                    spriteMax.y >= CameraMin.y &&
                    spriteMin.y <= CameraMax.y;

                cullState.ValueRW = isVisible;
            }
        }

        public void OnCreate(ref SystemState state)
        {
            spriteCullQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalToWorld, SpriteCullState>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(ref state);
            movedSpriteCullQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalToWorld, SpriteCullState>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
                .Build(ref state);
            movedSpriteCullQuery.SetChangedVersionFilter(ComponentType.ReadOnly<LocalToWorld>());
            disabledSpriteCullQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalToWorld>()
                .WithDisabled<SpriteCullState>()
                .Build(ref state);

            state.RequireForUpdate(spriteCullQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            bool cullingEnabled = !SystemAPI.HasSingleton<SpriteCullingSettings>() || SystemAPI.GetSingleton<SpriteCullingSettings>().Enabled != 0;
            if (SpriteCullingRuntime.TryGetOverride(out bool overrideEnabled))
            {
                cullingEnabled = overrideEnabled;
            }

            Camera camera = Camera.main;
            bool canCull = cullingEnabled && camera != null && camera.orthographic;

            if (!canCull)
            {
                if (!disabledSpriteCullQuery.IsEmptyIgnoreFilter)
                {
                    state.Dependency.Complete();
                    state.EntityManager.SetComponentEnabled<SpriteCullState>(spriteCullQuery, true);
                }

                hasCameraSnapshot = false;
                cachedCameraInstanceId = 0;
                return;
            }

            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * camera.aspect;
            Vector3 cameraPosition = camera.transform.position;
            float2 cameraPosition2D = new float2(cameraPosition.x, cameraPosition.y);
            bool cameraChanged = !hasCameraSnapshot
                || cachedCameraInstanceId != camera.GetInstanceID()
                || math.distancesq(cameraPosition2D, cachedCameraPosition) > 0.000001f
                || math.abs(camera.orthographicSize - cachedOrthographicSize) > 0.000001f
                || math.abs(camera.aspect - cachedAspect) > 0.000001f;

            EntityQuery query = cameraChanged ? spriteCullQuery : movedSpriteCullQuery;

            state.Dependency = new UpdateSpriteCullStateJob
            {
                CameraMin = new float2(cameraPosition.x - halfWidth, cameraPosition.y - halfHeight),
                CameraMax = new float2(cameraPosition.x + halfWidth, cameraPosition.y + halfHeight),
                Padding = EdgePadding
            }.ScheduleParallel(query, state.Dependency);

            hasCameraSnapshot = true;
            cachedCameraInstanceId = camera.GetInstanceID();
            cachedCameraPosition = cameraPosition2D;
            cachedOrthographicSize = camera.orthographicSize;
            cachedAspect = camera.aspect;
        }
    }
}
