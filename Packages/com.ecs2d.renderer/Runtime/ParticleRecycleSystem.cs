using Unity.Burst;
using Unity.Entities;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ParticleEmissionSystem))]
    public partial struct ParticleRecycleSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var (availableParticles, recycleParticles) in SystemAPI.Query<
                DynamicBuffer<ParticleEmitterAvailableParticleElement>,
                DynamicBuffer<ParticleEmitterRecycleParticleElement>>())
            {
                for (int i = 0; i < recycleParticles.Length; i++)
                {
                    availableParticles.Add(new ParticleEmitterAvailableParticleElement
                    {
                        Value = recycleParticles[i].Value
                    });
                }

                recycleParticles.Clear();
            }
        }
    }
}
