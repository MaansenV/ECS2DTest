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
        [BurstCompile]
        private partial struct SimulateActiveParticlesJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(
                ref ParticleRuntime runtime,
                ref SpriteData spriteData,
                ref LocalToWorld localToWorld,
                EnabledRefRW<ParticleActive> active,
                EnabledRefRW<ParticleResting> resting,
                EnabledRefRW<SpriteCullState> cullState)
            {
                if (!active.ValueRO)
                {
                    return;
                }

                runtime.Age += DeltaTime;
                if (runtime.Age >= runtime.Lifetime)
                {
                    runtime.LifecycleState = (byte)ParticleLifecycleState.Inactive;
                    runtime.CurrentSpeed = 0f;
                    runtime.Velocity = float2.zero;
                    spriteData.Scale = 0f;
                    spriteData.Color = float4.zero;
                    active.ValueRW = false;
                    resting.ValueRW = false;
                    cullState.ValueRW = false;
                    return;
                }

                float speedMultiplier = ParticleSpawnUtility.EvaluateSpeedMultiplier(runtime.Age, runtime.RestAfterSeconds);
                float nextSpeed = runtime.InitialSpeed * speedMultiplier;
                float2 movement = runtime.Velocity * speedMultiplier * DeltaTime;
                runtime.Position += new float3(movement, 0f);
                runtime.RotationRadians += runtime.RotationSpeedRadians * DeltaTime;
                runtime.CurrentSpeed = nextSpeed;

                ParticleSpawnUtility.WriteRenderState(ref runtime, ref spriteData, ref localToWorld);

                if (runtime.RestAfterSeconds > 0f && runtime.Age >= runtime.RestAfterSeconds)
                {
                    runtime.LifecycleState = (byte)ParticleLifecycleState.Resting;
                    runtime.CurrentSpeed = 0f;
                    runtime.Velocity = float2.zero;
                    active.ValueRW = false;
                    resting.ValueRW = true;
                }
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SimulateActiveParticlesJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }
    }
}
