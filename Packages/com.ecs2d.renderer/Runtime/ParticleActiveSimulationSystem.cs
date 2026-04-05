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

            foreach (var (runtime, spriteData, localToWorld, active, resting, cullState) in SystemAPI.Query<
                RefRW<ParticleRuntime>,
                RefRW<SpriteData>,
                RefRW<LocalToWorld>,
                EnabledRefRW<ParticleActive>,
                EnabledRefRW<ParticleResting>,
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
                    renderData.Color = float4.zero;
                    runtime.ValueRW = particleRuntime;
                    spriteData.ValueRW = renderData;
                    active.ValueRW = false;
                    resting.ValueRW = false;
                    cullState.ValueRW = false;
                    continue;
                }

                float speedMultiplier = ParticleSpawnUtility.EvaluateSpeedMultiplier(particleRuntime.Age, particleRuntime.RestAfterSeconds);
                float nextSpeed = particleRuntime.InitialSpeed * speedMultiplier;
                float2 movement = particleRuntime.Velocity * speedMultiplier * deltaTime;
                particleRuntime.Position += new float3(movement, 0f);
                particleRuntime.RotationRadians += particleRuntime.RotationSpeedRadians * deltaTime;
                particleRuntime.CurrentSpeed = nextSpeed;

                ParticleSpawnUtility.WriteRenderState(ref particleRuntime, ref renderData, ref currentLocalToWorld);

                if (particleRuntime.RestAfterSeconds > 0f && particleRuntime.Age >= particleRuntime.RestAfterSeconds)
                {
                    particleRuntime.LifecycleState = (byte)ParticleLifecycleState.Resting;
                    particleRuntime.CurrentSpeed = 0f;
                    particleRuntime.Velocity = float2.zero;
                    active.ValueRW = false;
                    resting.ValueRW = true;
                }

                runtime.ValueRW = particleRuntime;
                spriteData.ValueRW = renderData;
                localToWorld.ValueRW = currentLocalToWorld;
            }
        }
    }
}
