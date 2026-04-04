using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering.Tests
{
    public sealed class ParticleSystemTests
    {
        [Test]
        public void ParticleEmissionSystem_SpawnsBurstParticles_AndAssignsVaryingSpeeds()
        {
            using var world = new World("ParticleEmissionSystemTests");
            var entityManager = world.EntityManager;
            var emissionSystem = world.CreateSystem<ParticleEmissionSystem>();
            Entity emitter = CreateEmitterWithPool(world, maxParticles: 4, burstCount: 3, spawnRate: 0f, restAfterSeconds: -1f);

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
        public void ParticleActiveSimulationSystem_TransitionsParticlesToResting()
        {
            using var world = new World("ParticleRestingTransitionTests");
            var entityManager = world.EntityManager;
            var activeSystem = world.CreateSystem<ParticleActiveSimulationSystem>();
            Entity particle = CreateParticle(world, activeEnabled: true, restingEnabled: false);

            entityManager.SetComponentData(particle, new ParticleRuntime
            {
                Position = new float3(0f, 0f, 0f),
                Velocity = new float2(2f, 0f),
                Age = 0.4f,
                Lifetime = 5f,
                RestAfterSeconds = 0.5f,
                RotationRadians = 0f,
                RotationSpeedRadians = 0f,
                InitialSpeed = 2f,
                CurrentSpeed = 2f,
                StartScale = 1f,
                EndScale = 0f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                LifecycleState = (byte)ParticleLifecycleState.Active
            });
            world.SetTime(new TimeData(0.5, 0.5f));

            activeSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(particle);
            Assert.AreEqual((byte)ParticleLifecycleState.Resting, runtime.LifecycleState);
            Assert.IsFalse(entityManager.IsComponentEnabled<ParticleActive>(particle));
            Assert.IsTrue(entityManager.IsComponentEnabled<ParticleResting>(particle));
            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(particle));
        }

        [Test]
        public void ParticleRestingExpirySystem_DisablesExpiredRestingParticles()
        {
            using var world = new World("ParticleRestingExpiryTests");
            var entityManager = world.EntityManager;
            var expirySystem = world.CreateSystem<ParticleRestingExpirySystem>();
            Entity emitter = entityManager.CreateEntity(typeof(ParticleEmitter), typeof(ParticleEmitterRuntimeState));
            entityManager.SetComponentData(emitter, new ParticleEmitterRuntimeState
            {
                RestingExpiryAccumulator = 0.3f
            });

            Entity particle = CreateParticle(world, activeEnabled: false, restingEnabled: true);
            entityManager.SetComponentData(particle, new ParticleRuntime
            {
                Position = new float3(0f, 0f, 0f),
                Age = 0.9f,
                Lifetime = 1f,
                RestAfterSeconds = 0.1f,
                StartScale = 1f,
                EndScale = 0f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                LifecycleState = (byte)ParticleLifecycleState.Resting
            });

            expirySystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            ParticleRuntime runtime = entityManager.GetComponentData<ParticleRuntime>(particle);
            Assert.AreEqual((byte)ParticleLifecycleState.Inactive, runtime.LifecycleState);
            Assert.IsFalse(entityManager.IsComponentEnabled<ParticleResting>(particle));
            Assert.IsFalse(entityManager.IsComponentEnabled<SpriteCullState>(particle));
        }

        [Test]
        public void ParticleRestingExpirySystem_UpdatesScaleAndColorDuringRestSweep()
        {
            using var world = new World("ParticleRestingUpdateTests");
            var entityManager = world.EntityManager;
            var expirySystem = world.CreateSystem<ParticleRestingExpirySystem>();
            Entity emitter = entityManager.CreateEntity(typeof(ParticleEmitter), typeof(ParticleEmitterRuntimeState));
            entityManager.SetComponentData(emitter, new ParticleEmitterRuntimeState
            {
                RestingExpiryAccumulator = 0.25f
            });

            Entity particle = CreateParticle(world, activeEnabled: false, restingEnabled: true);
            entityManager.SetComponentData(particle, new ParticleRuntime
            {
                Position = new float3(0f, 0f, 0f),
                Age = 0.25f,
                Lifetime = 1f,
                RestAfterSeconds = 0.1f,
                StartScale = 1f,
                EndScale = 0f,
                StartColor = new float4(1f, 1f, 1f, 1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                LifecycleState = (byte)ParticleLifecycleState.Resting
            });

            expirySystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(particle);
            Assert.That(spriteData.Scale, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(spriteData.Color.w, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(particle));
        }

        private static Entity CreateEmitterWithPool(World world, int maxParticles, int burstCount, float spawnRate, float restAfterSeconds)
        {
            var entityManager = world.EntityManager;
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
                StartScale = 1f,
                EndScale = 0f,
                StartColor = new float4(1f),
                EndColor = new float4(1f, 1f, 1f, 0f),
                CircleRadius = 1f,
                FixedDirection = new float2(1f, 0f),
                StartRotationMinRadians = 0f,
                StartRotationMaxRadians = 0.5f,
                RotationSpeedMinRadians = 0f,
                RotationSpeedMaxRadians = 0.5f,
                RestAfterSeconds = restAfterSeconds,
                CircleMode = (byte)ParticleCircleMode.Area,
                DirectionMode = (byte)ParticleDirectionMode.Random,
                EmitBurstOnStart = 1
            });
            entityManager.SetComponentData(emitter, new ParticleEmitterRuntimeState
            {
                RandomState = 123u
            });

            entityManager.AddBuffer<ParticleEmitterParticleElement>(emitter);
            for (int i = 0; i < maxParticles; i++)
            {
                Entity particle = CreateParticle(world, activeEnabled: false, restingEnabled: false);
                SpriteData spriteData = entityManager.GetComponentData<SpriteData>(particle);
                spriteData.SpriteSheetId = 7;
                spriteData.SortingLayer = 0;
                entityManager.SetComponentData(particle, spriteData);
                DynamicBuffer<ParticleEmitterParticleElement> pool = entityManager.GetBuffer<ParticleEmitterParticleElement>(emitter);
                pool.Add(new ParticleEmitterParticleElement { Value = particle });
            }

            return emitter;
        }

        private static Entity CreateParticle(World world, bool activeEnabled, bool restingEnabled)
        {
            var entityManager = world.EntityManager;
            Entity particle = entityManager.CreateEntity(
                typeof(ParticleRuntime),
                typeof(ParticleActive),
                typeof(ParticleResting),
                typeof(LocalToWorld),
                typeof(SpriteData),
                typeof(SpriteCullState),
                typeof(SpriteSheetRenderKey));

            entityManager.SetComponentData(particle, new LocalToWorld
            {
                Value = float4x4.identity
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
            entityManager.SetComponentEnabled<ParticleResting>(particle, restingEnabled);
            entityManager.SetComponentEnabled<SpriteCullState>(particle, activeEnabled || restingEnabled);
            return particle;
        }
    }
}
