using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ParticleEmissionSystem))]
    public partial struct ParticleActiveSimulationSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleRuntime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (runtime, spriteData, localToWorld, active, cullState) in SystemAPI.Query<
                RefRW<ParticleRuntime>,
                RefRW<SpriteData>,
                RefRW<LocalToWorld>,
                EnabledRefRW<ParticleActive>,
                EnabledRefRW<SpriteCullState>>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState))
            {
                if (!active.ValueRO)
                {
                    continue;
                }

                ParticleRuntime particleRuntime = runtime.ValueRW;
                SpriteData renderData = spriteData.ValueRW;
                LocalToWorld currentLocalToWorld = localToWorld.ValueRW;

                particleRuntime.Age += deltaTime;
                if (particleRuntime.Age >= particleRuntime.Lifetime)
                {
                    particleRuntime.LifecycleState = (byte)ParticleLifecycleState.Inactive;
                    particleRuntime.CurrentSpeed = 0f;
                    particleRuntime.Velocity = float2.zero;
                    renderData.Scale = 0f;
                    renderData.ScaleXY = float2.zero;
                    renderData.Color = float4.zero;
                    runtime.ValueRW = particleRuntime;
                    spriteData.ValueRW = renderData;
                    active.ValueRW = false;
                    cullState.ValueRW = false;
                    continue;
                }

                float normalizedAge = ParticleSpawnUtility.EvaluateLifetimeFraction(particleRuntime.Age, particleRuntime.Lifetime);
                float speedMultiplier = ParticleSpawnUtility.EvaluateCurveLUT(particleRuntime.SpeedCurve, normalizedAge);
                float nextSpeed = particleRuntime.InitialSpeed * speedMultiplier;
                float2 movement = particleRuntime.Velocity * speedMultiplier * deltaTime;
                particleRuntime.Position += new float3(movement, 0f);
                particleRuntime.RotationRadians += particleRuntime.RotationSpeedRadians * deltaTime;
                particleRuntime.CurrentSpeed = nextSpeed;

                ParticleSpawnUtility.WriteRenderState(ref particleRuntime, ref renderData, ref currentLocalToWorld);

                runtime.ValueRW = particleRuntime;
                spriteData.ValueRW = renderData;
                localToWorld.ValueRW = currentLocalToWorld;
            }
        }
    }
}
