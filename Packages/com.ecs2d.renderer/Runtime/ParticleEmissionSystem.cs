using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ParticleRecycleSystem))]
    public partial struct ParticleEmissionSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ParticleEmitter>();
        }

        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var runtimeLookup = SystemAPI.GetComponentLookup<ParticleRuntime>();
            var spriteDataLookup = SystemAPI.GetComponentLookup<SpriteData>();
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();
            var particleActiveLookup = SystemAPI.GetComponentLookup<ParticleActive>();
            var spriteCullLookup = SystemAPI.GetComponentLookup<SpriteCullState>();

            foreach (var (emitter, runtimeState, localToWorld, availableParticles) in SystemAPI
                .Query<RefRO<ParticleEmitter>, RefRW<ParticleEmitterRuntimeState>, RefRO<LocalToWorld>, DynamicBuffer<ParticleEmitterAvailableParticleElement>>())
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
                    if (availableParticles.Length <= 0)
                    {
                        break;
                    }

                    int lastIndex = availableParticles.Length - 1;
                    Entity particleEntity = availableParticles[lastIndex].Value;
                    availableParticles.RemoveAt(lastIndex);

                    SpawnParticle(
                        particleEntity,
                        in emitterConfig,
                        in localToWorld.ValueRO,
                        ref random,
                        ref runtimeLookup,
                        ref spriteDataLookup,
                        ref localToWorldLookup,
                        ref particleActiveLookup,
                        ref spriteCullLookup);
                }

                emitterState.RandomState = random.state;
            }
        }

        private static void SpawnParticle(
            Entity particleEntity,
            in ParticleEmitter emitter,
            in LocalToWorld emitterLocalToWorld,
            ref Random random,
            ref ComponentLookup<ParticleRuntime> runtimeLookup,
            ref ComponentLookup<SpriteData> spriteDataLookup,
            ref ComponentLookup<LocalToWorld> localToWorldLookup,
            ref ComponentLookup<ParticleActive> particleActiveLookup,
            ref ComponentLookup<SpriteCullState> spriteCullLookup)
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

            ParticleRuntime runtime = runtimeLookup[particleEntity];
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
            runtime.BaseScaleXY = emitter.BaseScaleXY;
            runtime.StartColor = emitter.StartColor;
            runtime.EndColor = emitter.EndColor;
            runtime.LifecycleState = (byte)ParticleLifecycleState.Active;

            SpriteData spriteData = spriteDataLookup[particleEntity];
            spriteData.SpriteFrameIndex = emitter.SpriteFrameIndex;
            spriteData.SpriteSheetId = emitter.SheetId;
            spriteData.SortingLayer = emitter.SortingLayer;
            spriteData.BaseScale = 1f;
            spriteData.BaseScaleXY = new float2(1f, 1f);
            spriteData.RotationOffsetRadians = 0f;

            LocalToWorld localToWorld = localToWorldLookup[particleEntity];
            ParticleSpawnUtility.WriteRenderState(ref runtime, ref spriteData, ref localToWorld);

            runtimeLookup[particleEntity] = runtime;
            spriteDataLookup[particleEntity] = spriteData;
            localToWorldLookup[particleEntity] = localToWorld;
            particleActiveLookup.SetComponentEnabled(particleEntity, true);
            spriteCullLookup.SetComponentEnabled(particleEntity, true);
        }
    }
}
