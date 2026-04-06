using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct ParticleEmissionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var entityManager = state.EntityManager;

            foreach (var (emitter, runtimeState, localToWorld, pool) in SystemAPI
                .Query<RefRO<ParticleEmitter>, RefRW<ParticleEmitterRuntimeState>, RefRO<LocalToWorld>, DynamicBuffer<ParticleEmitterParticleElement>>())
            {
                int spawnCount = 0;
                ref ParticleEmitterRuntimeState emitterState = ref runtimeState.ValueRW;
                ref readonly ParticleEmitter emitterConfig = ref emitter.ValueRO;

                if (emitterConfig.EmitBurstOnStart != 0 && emitterState.BurstConsumed == 0)
                {
                    spawnCount += emitterConfig.BurstCount;
                    emitterState.BurstConsumed = 1;
                }

                emitterState.SpawnAccumulator += emitterConfig.SpawnRate * deltaTime;
                if (emitterState.SpawnAccumulator >= 1f)
                {
                    int rateSpawnCount = (int)math.floor(emitterState.SpawnAccumulator);
                    spawnCount += rateSpawnCount;
                    emitterState.SpawnAccumulator -= rateSpawnCount;
                }

                if (spawnCount <= 0)
                {
                    continue;
                }

                var random = new Random(emitterState.RandomState == 0u ? 1u : emitterState.RandomState);

                for (int i = 0; i < spawnCount; i++)
                {
                    if (!TryAcquireInactiveParticle(entityManager, pool, ref emitterState, out Entity particleEntity))
                    {
                        break;
                    }

                    SpawnParticle(entityManager, particleEntity, in emitterConfig, in localToWorld.ValueRO, ref random);
                }

                emitterState.RandomState = random.state;
            }
        }

        private static bool TryAcquireInactiveParticle(
            EntityManager entityManager,
            DynamicBuffer<ParticleEmitterParticleElement> pool,
            ref ParticleEmitterRuntimeState emitterState,
            out Entity particleEntity)
        {
            int count = pool.Length;
            for (int i = 0; i < count; i++)
            {
                int index = (emitterState.NextPoolIndex + i) % count;
                Entity candidate = pool[index].Value;
                ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(candidate);
                if ((ParticleLifecycleState)runtime.LifecycleState != ParticleLifecycleState.Inactive)
                {
                    continue;
                }

                emitterState.NextPoolIndex = (index + 1) % count;
                particleEntity = candidate;
                return true;
            }

            particleEntity = Entity.Null;
            return false;
        }

        private static void SpawnParticle(
            EntityManager entityManager,
            Entity particleEntity,
            in ParticleEmitter emitter,
            in LocalToWorld emitterLocalToWorld,
            ref Random random)
        {
            float2 localOffset = ParticleSpawnUtility.SampleCircleOffset(ref random, emitter.CircleRadius, (ParticleCircleMode)emitter.CircleMode);
            float2 localDirection = ParticleSpawnUtility.ResolveDirection(
                ref random,
                (ParticleDirectionMode)emitter.DirectionMode,
                localOffset,
                emitter.FixedDirection);

            float3 worldPosition = ParticleSpawnUtility.TransformPoint(emitterLocalToWorld, localOffset);
            float2 worldDirection = ParticleSpawnUtility.TransformDirection(emitterLocalToWorld, localDirection);
            float lifetime = ParticleSpawnUtility.SampleRange(ref random, emitter.LifetimeMin, emitter.LifetimeMax);
            float initialSpeed = ParticleSpawnUtility.SampleRange(ref random, emitter.SpeedMin, emitter.SpeedMax);
            float rotationRadians = ParticleSpawnUtility.SampleRange(ref random, emitter.StartRotationMinRadians, emitter.StartRotationMaxRadians);
            float rotationSpeedRadians = ParticleSpawnUtility.SampleRange(ref random, emitter.RotationSpeedMinRadians, emitter.RotationSpeedMaxRadians);

            ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(particleEntity);
            runtime.Position = worldPosition;
            runtime.Velocity = worldDirection * initialSpeed;
            runtime.Age = 0f;
            runtime.Lifetime = lifetime;
            runtime.SpeedCurve = emitter.SpeedCurve;
            runtime.ScaleCurve = emitter.ScaleCurve;
            runtime.RotationRadians = rotationRadians;
            runtime.RotationSpeedRadians = rotationSpeedRadians;
            runtime.InitialSpeed = initialSpeed;
            runtime.CurrentSpeed = initialSpeed;
            runtime.BaseScale = emitter.BaseScale;
            runtime.StartColor = emitter.StartColor;
            runtime.EndColor = emitter.EndColor;
            runtime.LifecycleState = (byte)ParticleLifecycleState.Active;

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(particleEntity);
            spriteData.SpriteFrameIndex = emitter.SpriteFrameIndex;
            spriteData.SpriteSheetId = emitter.SheetId;
            spriteData.SortingLayer = emitter.SortingLayer;
            spriteData.BaseScale = 1f;
            spriteData.RotationOffsetRadians = 0f;

            LocalToWorld localToWorld = entityManager.GetComponentData<LocalToWorld>(particleEntity);
            ParticleSpawnUtility.WriteRenderState(ref runtime, ref spriteData, ref localToWorld);

            entityManager.SetComponentData(particleEntity, runtime);
            entityManager.SetComponentData(particleEntity, spriteData);
            entityManager.SetComponentData(particleEntity, localToWorld);
            entityManager.SetComponentEnabled<ParticleActive>(particleEntity, true);
            entityManager.SetComponentEnabled<SpriteCullState>(particleEntity, true);
        }
    }
}
