using System.Collections.Generic;
using NUnit.Framework;
using ECS2D.Rendering;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering.Tests
{
    public sealed class ParticleSystemTests
    {
        private readonly List<BlobAssetReference<CurveBlobLUT>> _curveBlobs = new();

        [TearDown]
        public void TearDown()
        {
            foreach (BlobAssetReference<CurveBlobLUT> curveBlob in _curveBlobs)
            {
                if (curveBlob.IsCreated)
                {
                    curveBlob.Dispose();
                }
            }

            _curveBlobs.Clear();
        }

        [Test]
        public void ParticleEmissionSystem_SpawnsBurstParticles_AndAssignsVaryingSpeeds()
        {
            using var world = new World("ParticleEmissionSystemTests");
            var entityManager = world.EntityManager;
            var emissionSystem = world.CreateSystem<ParticleEmissionSystem>();
            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 3, spawnRate: 0f);

            emissionSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
            int activeCount = 0;
            using var speeds = new NativeList<float>(Allocator.Temp);

            foreach (var element in pool)
            {
                if (!entityManager.IsComponentEnabled<ParticleActive>(element.Value))
                {
                    continue;
                }

                activeCount++;
                speeds.Add(entityManager.GetComponentData<ParticleRuntime>(element.Value).InitialSpeed);
            }

            Assert.AreEqual(3, activeCount);
            Assert.That(speeds[0], Is.Not.EqualTo(speeds[1]).Within(0.0001f));
            Assert.That(speeds[1], Is.Not.EqualTo(speeds[2]).Within(0.0001f));
        }

        [Test]
        public void ParticleEmissionSystem_SpawnedParticleHasCurveBlobRefs()
        {
            using var world = new World("ParticleCurveBlobSpawnTests");
            var entityManager = world.EntityManager;
            var emissionSystem = world.CreateSystem<ParticleEmissionSystem>();
            Entity emitter = CreateEmitterWithPool(world, maxParticles: 1, burstCount: 1, spawnRate: 0f);

            emissionSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Entity particle = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter)[0].Value;
            ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(particle);

            Assert.IsTrue(runtime.SpeedCurve.IsCreated);
            Assert.IsTrue(runtime.ScaleCurve.IsCreated);
        }

        [Test]
        public void ParticleActiveSimulationSystem_TransitionsParticlesToInactiveAtEndOfLifetime()
        {
            using var world = new World("ParticleLifetimeExpiryTests");
            var entityManager = world.EntityManager;
            var activeSystem = world.CreateSystem<ParticleActiveSimulationSystem>();
            Entity particle = CreateParticle(world, activeEnabled: true);

            entityManager.SetComponentData(particle, new ParticleRuntime
            {
                Position = new float3(0f, 0f, 0f),
                Velocity = new float2(2f, 0f),
                Age = 0.4f,
                Lifetime = 0.5f,
                SpeedCurve = CreateTrackedFlatCurveBlob(1f),
                ScaleCurve = CreateTrackedFlatCurveBlob(1f),
                RotationRadians = 0f,
                RotationSpeedRadians = 0f,
                InitialSpeed = 2f,
                CurrentSpeed = 2f,
                BaseScale = 1f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                LifecycleState = (byte)ParticleLifecycleState.Active
            });
            world.SetTime(new TimeData(0.5, 0.5f));

            activeSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(particle);
            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(particle);

            Assert.AreEqual((byte)ParticleLifecycleState.Inactive, runtime.LifecycleState);
            Assert.IsFalse(entityManager.IsComponentEnabled<ParticleActive>(particle));
            Assert.IsFalse(entityManager.IsComponentEnabled<SpriteCullState>(particle));
            Assert.That(runtime.CurrentSpeed, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(runtime.Velocity.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(runtime.Velocity.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(spriteData.Color, Is.EqualTo(float4.zero));
        }

        [Test]
        public void ParticleActiveSimulationSystem_SpeedFollowsCurveOverLifetime()
        {
            using var world = new World("ParticleSpeedCurveTests");
            var entityManager = world.EntityManager;
            var activeSystem = world.CreateSystem<ParticleActiveSimulationSystem>();
            Entity particle = CreateParticle(world, activeEnabled: true);

            BlobAssetReference<CurveBlobLUT> speedCurve = CreateTrackedLinearRampCurveBlob();
            BlobAssetReference<CurveBlobLUT> scaleCurve = CreateTrackedFlatCurveBlob(1f);

            void SetParticleAge(float age)
            {
                entityManager.SetComponentEnabled<ParticleActive>(particle, true);
                entityManager.SetComponentEnabled<SpriteCullState>(particle, true);
                entityManager.SetComponentData(particle, new ParticleRuntime
                {
                    Position = float3.zero,
                    Velocity = new float2(1f, 0f),
                    Age = age,
                    Lifetime = 2f,
                    SpeedCurve = speedCurve,
                    ScaleCurve = scaleCurve,
                    RotationRadians = 0f,
                    RotationSpeedRadians = 0f,
                    InitialSpeed = 10f,
                    CurrentSpeed = -1f,
                    BaseScale = 1f,
                    StartColor = new float4(1f),
                    EndColor = new float4(1f),
                    LifecycleState = (byte)ParticleLifecycleState.Active
                });
            }

            SetParticleAge(0f);
            world.SetTime(new TimeData(0d, 0f));
            activeSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();
            Assert.That(entityManager.GetComponentData<ParticleRuntime>(particle).CurrentSpeed, Is.EqualTo(0f).Within(0.02f));

            SetParticleAge(1f);
            world.SetTime(new TimeData(1d, 0f));
            activeSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();
            Assert.That(entityManager.GetComponentData<ParticleRuntime>(particle).CurrentSpeed, Is.EqualTo(5f).Within(0.02f));

            SetParticleAge(1.9999f);
            world.SetTime(new TimeData(2d, 0f));
            activeSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();
            Assert.That(entityManager.GetComponentData<ParticleRuntime>(particle).CurrentSpeed, Is.EqualTo(10f).Within(0.02f));
        }

        [Test]
        public void ParticleEmitterCleanupSystem_DoesNotDestroyDisabledEmitter()
        {
            using var world = new World("CleanupDisabledEmitterTests");
            var entityManager = world.EntityManager;
            var cleanupSystem = world.CreateSystem<ParticleEmitterCleanupSystem>();
            world.CreateSystem<EndSimulationEntityCommandBufferSystem>();

            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 0, spawnRate: 0f, destroyEmitterAfterSeconds: -1f);
            DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
            Entity[] poolEntities = new Entity[4];
            for (int i = 0; i < 4; i++)
            {
                poolEntities[i] = pool[i].Value;
            }

            for (int i = 0; i < 20; i++)
            {
                world.SetTime(new TimeData(0.5 * (i + 1), 0.5f));
                cleanupSystem.Update(world.Unmanaged);
                entityManager.CompleteAllTrackedJobs();
            }

            Assert.IsTrue(entityManager.Exists(emitter));
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(entityManager.Exists(poolEntities[i]));
            }
        }

        [Test]
        public void ParticleEmitterCleanupSystem_DestroysEmitterAfterTimer()
        {
            using var world = new World("CleanupTimerExpiryTests");
            var entityManager = world.EntityManager;
            var cleanupSystem = world.CreateSystem<ParticleEmitterCleanupSystem>();
            world.CreateSystem<EndSimulationEntityCommandBufferSystem>();

            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 0, spawnRate: 0f, destroyEmitterAfterSeconds: 1.0f);
            DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
            Entity[] poolEntities = new Entity[4];
            for (int i = 0; i < 4; i++)
            {
                poolEntities[i] = pool[i].Value;
            }

            world.SetTime(new TimeData(0.5, 0.5f));
            cleanupSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(entityManager.Exists(emitter));

            world.SetTime(new TimeData(1.0, 0.5f));
            cleanupSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsFalse(entityManager.Exists(emitter));
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(entityManager.Exists(poolEntities[i]));
            }
        }

        [Test]
        public void ParticleEmitterCleanupSystem_DestroysPoolEntitiesWithEmitter()
        {
            using var world = new World("CleanupPoolEntitiesTests");
            var entityManager = world.EntityManager;
            var cleanupSystem = world.CreateSystem<ParticleEmitterCleanupSystem>();
            world.CreateSystem<EndSimulationEntityCommandBufferSystem>();

            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 0, spawnRate: 0f, destroyEmitterAfterSeconds: 1.0f);
            DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
            Entity[] poolEntities = new Entity[4];
            for (int i = 0; i < 4; i++)
            {
                poolEntities[i] = pool[i].Value;
            }

            world.SetTime(new TimeData(1.0, 1.0f));
            cleanupSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsFalse(entityManager.Exists(emitter), "Emitter should be destroyed after timer expires");
            for (int i = 0; i < 4; i++)
            {
                Assert.IsFalse(entityManager.Exists(poolEntities[i]), $"Pool entity {i} should be destroyed with emitter");
            }
        }

        [Test]
        public void ParticleEmitterCleanupSystem_DoesNotDestroyBeforeTimer()
        {
            using var world = new World("CleanupBeforeTimerTests");
            var entityManager = world.EntityManager;
            var cleanupSystem = world.CreateSystem<ParticleEmitterCleanupSystem>();
            world.CreateSystem<EndSimulationEntityCommandBufferSystem>();

            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 0, spawnRate: 0f, destroyEmitterAfterSeconds: 2.0f);

            world.SetTime(new TimeData(1.5, 1.5f));
            cleanupSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(entityManager.Exists(emitter), "Emitter should still exist before timer expires");
        }

        private Entity CreateEmitterWithPool(World world, int maxParticles, int burstCount, float spawnRate, float destroyEmitterAfterSeconds = -1f)
        {
            var entityManager = world.EntityManager;
            BlobAssetReference<CurveBlobLUT> speedCurve = CreateTrackedFlatCurveBlob(1f);
            BlobAssetReference<CurveBlobLUT> scaleCurve = CreateTrackedFlatCurveBlob(1f);
            Entity emitter = entityManager.CreateEntity(typeof(LocalToWorld), typeof(ParticleEmitter), typeof(ParticleEmitterRuntimeState));
            entityManager.SetComponentData(emitter, new LocalToWorld
            {
                Value = float4x4.identity
            });
            entityManager.SetComponentData(emitter, new ParticleEmitter
            {
                SheetId = 7,
                SortingLayer = 0,
                SpriteFrameIndex = 0,
                MaxParticles = maxParticles,
                BurstCount = burstCount,
                SpawnRate = spawnRate,
                LifetimeMin = 1f,
                LifetimeMax = 2f,
                SpeedMin = 1f,
                SpeedMax = 3f,
                SpeedCurve = speedCurve,
                ScaleCurve = scaleCurve,
                BaseScale = 1f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                CircleRadius = 1f,
                FixedDirection = new float2(1f, 0f),
                StartRotationMinRadians = 0f,
                StartRotationMaxRadians = 0.5f,
                RotationSpeedMinRadians = 0f,
                RotationSpeedMaxRadians = 0.5f,
                DestroyEmitterAfterSeconds = destroyEmitterAfterSeconds,
                CircleMode = (byte)ParticleCircleMode.Area,
                DirectionMode = (byte)ParticleDirectionMode.Random,
                SpeedCurveMode = (byte)ParticleCurveMode.Constant,
                ScaleCurveMode = (byte)ParticleCurveMode.Constant,
                EmitBurstOnStart = 1
            });
            entityManager.SetComponentData(emitter, new ParticleEmitterRuntimeState
            {
                RandomState = 123u
            });

            entityManager.AddBuffer<ParticleEmitterParticleElement>(emitter);
            for (int i = 0; i < maxParticles; i++)
            {
                Entity particle = CreateParticle(world, activeEnabled: false);
                SpriteData spriteData = entityManager.GetComponentData<SpriteData>(particle);
                spriteData.SpriteSheetId = 7;
                spriteData.SortingLayer = 0;
                entityManager.SetComponentData(particle, spriteData);
                DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
                pool.Add(new ParticleEmitterParticleElement { Value = particle });
            }

            return emitter;
        }

        private Entity CreateParticle(World world, bool activeEnabled)
        {
            var entityManager = world.EntityManager;
            BlobAssetReference<CurveBlobLUT> speedCurve = CreateTrackedFlatCurveBlob(1f);
            BlobAssetReference<CurveBlobLUT> scaleCurve = CreateTrackedFlatCurveBlob(1f);
            Entity particle = entityManager.CreateEntity(
                typeof(ParticleRuntime),
                typeof(ParticleActive),
                typeof(LocalToWorld),
                typeof(SpriteData),
                typeof(SpriteCullState),
                typeof(SpriteSheetRenderKey));

            entityManager.SetComponentData(particle, new LocalToWorld
            {
                Value = float4x4.identity
            });
            entityManager.SetComponentData(particle, new ParticleRuntime
            {
                SpeedCurve = speedCurve,
                ScaleCurve = scaleCurve,
                BaseScale = 1f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                LifecycleState = (byte)ParticleLifecycleState.Inactive
            });
            entityManager.SetComponentData(particle, new SpriteData
            {
                TranslationAndRotation = float4.zero,
                BaseScale = 1f,
                Scale = 1f,
                Color = new float4(1f),
                SpriteFrameIndex = 0,
                SpriteSheetId = 7,
                SortingLayer = 0
            });
            entityManager.SetSharedComponent(particle, new SpriteSheetRenderKey { SheetId = 7 });
            entityManager.SetComponentEnabled<ParticleActive>(particle, activeEnabled);
            entityManager.SetComponentEnabled<SpriteCullState>(particle, activeEnabled);
            return particle;
        }

        private BlobAssetReference<CurveBlobLUT> CreateTrackedFlatCurveBlob(float value)
        {
            BlobAssetReference<CurveBlobLUT> blob = ParticleSpawnUtility.CreateFlatCurveBlob(CurveBlobLUT.kSampleCount, value);
            _curveBlobs.Add(blob);
            return blob;
        }

        private BlobAssetReference<CurveBlobLUT> CreateTrackedLinearRampCurveBlob()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref CurveBlobLUT root = ref builder.ConstructRoot<CurveBlobLUT>();
            BlobBuilderArray<float> samples = builder.Allocate(ref root.Samples, CurveBlobLUT.kSampleCount);

            for (int i = 0; i < CurveBlobLUT.kSampleCount; i++)
            {
                samples[i] = i / 63f;
            }

            BlobAssetReference<CurveBlobLUT> blob = builder.CreateBlobAssetReference<CurveBlobLUT>(Allocator.Persistent);
            _curveBlobs.Add(blob);
            return blob;
        }
    }
}
