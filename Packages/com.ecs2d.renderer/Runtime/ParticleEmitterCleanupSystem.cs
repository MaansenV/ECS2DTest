using Unity.Burst;
using Unity.Entities;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ParticleActiveSimulationSystem))]
    public partial struct ParticleEmitterCleanupSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (emitter, runtimeState, particleBuffer, entity) in SystemAPI.Query<
                RefRO<ParticleEmitter>,
                RefRW<ParticleEmitterRuntimeState>,
                DynamicBuffer<ParticleEmitterParticleElement>>()
                .WithEntityAccess())
            {
                if (emitter.ValueRO.DestroyEmitterAfterSeconds <= 0f)
                {
                    continue;
                }

                runtimeState.ValueRW.EmitterAge += deltaTime;

                if (runtimeState.ValueRO.EmitterAge < emitter.ValueRO.DestroyEmitterAfterSeconds)
                {
                    continue;
                }

                if (particleBuffer.Length > 0)
                {
                    ecb.DestroyEntity(particleBuffer.Reinterpret<Entity>().AsNativeArray());
                }

                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
