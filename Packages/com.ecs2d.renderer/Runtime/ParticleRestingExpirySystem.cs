using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ParticleActiveSimulationSystem))]
    public partial struct ParticleRestingExpirySystem : ISystem
    {
        private const float RestingCheckInterval = 0.25f;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var entityManager = state.EntityManager;

            foreach (var runtimeState in SystemAPI.Query<RefRW<ParticleEmitterRuntimeState>>())
            {
                runtimeState.ValueRW.RestingExpiryAccumulator += deltaTime;
            }

            if (deltaTime < RestingCheckInterval)
            {
                bool anyEmitterReady = false;
                foreach (var runtimeState in SystemAPI.Query<RefRO<ParticleEmitterRuntimeState>>())
                {
                    if (runtimeState.ValueRO.RestingExpiryAccumulator >= RestingCheckInterval)
                    {
                        anyEmitterReady = true;
                        break;
                    }
                }

                if (!anyEmitterReady)
                {
                    return;
                }
            }

            float restingDelta = 0f;
            foreach (var runtimeState in SystemAPI.Query<RefRW<ParticleEmitterRuntimeState>>())
            {
                restingDelta = math.max(restingDelta, runtimeState.ValueRO.RestingExpiryAccumulator);
                runtimeState.ValueRW.RestingExpiryAccumulator = 0f;
            }

            if (restingDelta <= 0f)
            {
                return;
            }

            foreach (var (runtime, spriteData, localToWorld, resting, cullState) in SystemAPI.Query<
                RefRW<ParticleRuntime>,
                RefRW<SpriteData>,
                RefRW<LocalToWorld>,
                EnabledRefRW<ParticleResting>,
                EnabledRefRW<SpriteCullState>>())
            {
                if (!resting.ValueRO)
                {
                    continue;
                }

                runtime.ValueRW.Age += restingDelta;
                ParticleRuntime particleRuntime = runtime.ValueRW;
                SpriteData renderData = spriteData.ValueRW;
                LocalToWorld currentLocalToWorld = localToWorld.ValueRW;
                ParticleSpawnUtility.WriteRenderState(ref particleRuntime, ref renderData, ref currentLocalToWorld);
                runtime.ValueRW = particleRuntime;
                spriteData.ValueRW = renderData;
                localToWorld.ValueRW = currentLocalToWorld;

                if (runtime.ValueRO.Age < runtime.ValueRO.Lifetime)
                {
                    continue;
                }

                runtime.ValueRW.LifecycleState = (byte)ParticleLifecycleState.Inactive;
                runtime.ValueRW.CurrentSpeed = 0f;
                runtime.ValueRW.Velocity = float2.zero;
                spriteData.ValueRW.Scale = 0f;
                spriteData.ValueRW.Color = float4.zero;
                resting.ValueRW = false;
                cullState.ValueRW = false;
            }
        }
    }
}
