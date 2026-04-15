using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace ECS2D.Rendering.Tests
{
    public sealed class ParticleAuthoringBakeTests
    {
        [Test]
        public void ParticleEmitterAuthoring_BakesEmitterAndPreallocatedPool()
        {
            using var world = new World("ParticleEmitterAuthoringBakeTests");
            using var blobAssetStore = new BlobAssetStore(128);

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var root = new GameObject("ParticleEmitterAuthoringBakeTests");

            try
            {
                SetField(sheet, "sheetId", 13);
                SetField(sheet, "autoGenerateGridFrames", false);
                SetField(sheet, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });

                var authoring = root.AddComponent<ParticleEmitterAuthoring>();
                authoring.SpriteSheet = sheet;
                authoring.MaxParticles = 4;
                authoring.BurstCount = 3;
                authoring.SpawnRate = 1.5f;
                authoring.CircleRadius = 2f;
                authoring.DirectionMode = ParticleDirectionMode.Random;

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, root);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                Entity emitterEntity = GetBakedEntity(bakingSystem, root);

                var emitter = world.EntityManager.GetComponentData<ParticleEmitter>(emitterEntity);
                DynamicBuffer<ParticleEmitterParticleElement> pool = world.EntityManager.GetBuffer<ParticleEmitterParticleElement>(emitterEntity);

                Assert.AreEqual(13, emitter.SheetId);
                Assert.AreEqual(4, emitter.MaxParticles);
                Assert.AreEqual(3, emitter.BurstCount);
                Assert.AreEqual(1.5f, emitter.SpawnRate, 0.0001f);
                Assert.IsTrue(IsBlobAssetReferenceCreated(emitter, nameof(ParticleEmitter.SpeedCurve)));
                Assert.IsTrue(IsBlobAssetReferenceCreated(emitter, nameof(ParticleEmitter.ScaleCurve)));
                Assert.AreEqual(4, pool.Length);

                Entity firstParticle = pool[0].Value;
                Assert.IsTrue(world.EntityManager.HasComponent<ParticleRuntime>(firstParticle));
                Assert.IsTrue(world.EntityManager.HasComponent<LocalToWorld>(firstParticle));
                Assert.IsFalse(world.EntityManager.IsComponentEnabled<ParticleActive>(firstParticle));
                Assert.IsFalse(world.EntityManager.IsComponentEnabled<SpriteCullState>(firstParticle));
                Assert.AreEqual(13, world.EntityManager.GetSharedComponent<SpriteSheetRenderKey>(firstParticle).SheetId);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void ParticleEmitterAuthoring_BakesCurveFields()
        {
            using var world = new World("ParticleEmitterAuthoringCurveBakeTests");
            using var blobAssetStore = new BlobAssetStore(128);

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var root = new GameObject("ParticleEmitterAuthoringCurveBakeTests");

            try
            {
                SetField(sheet, "sheetId", 34);
                SetField(sheet, "autoGenerateGridFrames", false);
                SetField(sheet, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });

                var authoring = root.AddComponent<ParticleEmitterAuthoring>();
                authoring.SpriteSheet = sheet;
                authoring.MaxParticles = 2;
                authoring.BaseScale = 1.75f;
                authoring.SpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
                authoring.ScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, root);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                Entity emitterEntity = GetBakedEntity(bakingSystem, root);
                var emitter = world.EntityManager.GetComponentData<ParticleEmitter>(emitterEntity);
                DynamicBuffer<ParticleEmitterParticleElement> pool = world.EntityManager.GetBuffer<ParticleEmitterParticleElement>(emitterEntity);
                var runtime = world.EntityManager.GetComponentData<ParticleRuntime>(pool[0].Value);

                Assert.IsTrue(IsBlobAssetReferenceCreated(emitter, nameof(ParticleEmitter.SpeedCurve)));
                Assert.IsTrue(IsBlobAssetReferenceCreated(emitter, nameof(ParticleEmitter.ScaleCurve)));
                Assert.AreEqual(authoring.BaseScale, emitter.BaseScale, 0.0001f);
                Assert.AreEqual((byte)ParticleCurveMode.Constant, emitter.SpeedCurveMode);
                Assert.AreEqual((byte)ParticleCurveMode.Constant, emitter.ScaleCurveMode);
                Assert.IsTrue(IsBlobAssetReferenceCreated(runtime, nameof(ParticleRuntime.SpeedCurve)));
                Assert.IsTrue(IsBlobAssetReferenceCreated(runtime, nameof(ParticleRuntime.ScaleCurve)));
                Assert.AreEqual(authoring.BaseScale, runtime.BaseScale, 0.0001f);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void ParticleEmitterAuthoring_AssignsDistinctNonZeroRandomSeedsPerEmitter()
        {
            using var world = new World("ParticleEmitterAuthoringRandomSeedBakeTests");
            using var blobAssetStore = new BlobAssetStore(128);

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var rootA = new GameObject("ParticleEmitterAuthoringRandomSeedBakeTests-A");
            var rootB = new GameObject("ParticleEmitterAuthoringRandomSeedBakeTests-B");

            try
            {
                SetField(sheet, "sheetId", 55);
                SetField(sheet, "autoGenerateGridFrames", false);
                SetField(sheet, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });

                var authoringA = rootA.AddComponent<ParticleEmitterAuthoring>();
                authoringA.SpriteSheet = sheet;
                authoringA.MaxParticles = 1;

                var authoringB = rootB.AddComponent<ParticleEmitterAuthoring>();
                authoringB.SpriteSheet = sheet;
                authoringB.MaxParticles = 1;

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, rootA, rootB);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                Entity emitterEntityA = GetBakedEntity(bakingSystem, rootA);
                Entity emitterEntityB = GetBakedEntity(bakingSystem, rootB);

                var runtimeStateA = world.EntityManager.GetComponentData<ParticleEmitterRuntimeState>(emitterEntityA);
                var runtimeStateB = world.EntityManager.GetComponentData<ParticleEmitterRuntimeState>(emitterEntityB);

                Assert.AreNotEqual(0u, runtimeStateA.RandomState);
                Assert.AreNotEqual(0u, runtimeStateB.RandomState);
                Assert.AreNotEqual(runtimeStateA.RandomState, runtimeStateB.RandomState);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootA);
                UnityEngine.Object.DestroyImmediate(rootB);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void ParticleEmitterAuthoring_BakedEmitterSpawnsAndCleansUp()
        {
            using var world = new World("ParticleEmitterAuthoringCleanupTests");
            using var blobAssetStore = new BlobAssetStore(128);

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var root = new GameObject("ParticleEmitterAuthoringCleanupTests");

            try
            {
                SetField(sheet, "sheetId", 21);
                SetField(sheet, "autoGenerateGridFrames", false);
                SetField(sheet, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });

                var authoring = root.AddComponent<ParticleEmitterAuthoring>();
                authoring.SpriteSheet = sheet;
                authoring.MaxParticles = 4;
                authoring.BurstCount = 2;
                authoring.SpawnRate = 0f;
                authoring.DestroyEmitterAfterSeconds = 1f;

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, root);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                Entity emitterEntity = GetBakedEntity(bakingSystem, root);
                DynamicBuffer<ParticleEmitterParticleElement> pool = world.EntityManager.GetBuffer<ParticleEmitterParticleElement>(emitterEntity);
                Entity[] poolEntities = new Entity[pool.Length];
                for (int i = 0; i < pool.Length; i++)
                {
                    poolEntities[i] = pool[i].Value;
                }

                var emissionSystem = world.CreateSystem<ParticleEmissionSystem>();
                var cleanupSystem = world.CreateSystem<ParticleEmitterCleanupSystem>();

                world.SetTime(new TimeData(0.5, 0.5f));
                emissionSystem.Update(world.Unmanaged);
                cleanupSystem.Update(world.Unmanaged);
                world.EntityManager.CompleteAllTrackedJobs();

                int activeCount = 0;
                for (int i = 0; i < poolEntities.Length; i++)
                {
                    if (world.EntityManager.IsComponentEnabled<ParticleActive>(poolEntities[i]))
                    {
                        activeCount++;
                    }
                }

                Assert.AreEqual(2, activeCount);
                Assert.IsTrue(world.EntityManager.Exists(emitterEntity));

                world.SetTime(new TimeData(1.0, 0.5f));
                cleanupSystem.Update(world.Unmanaged);
                world.EntityManager.CompleteAllTrackedJobs();

                Assert.IsFalse(world.EntityManager.Exists(emitterEntity));
                for (int i = 0; i < poolEntities.Length; i++)
                {
                    Assert.IsFalse(world.EntityManager.Exists(poolEntities[i]), $"Pool entity {i} should be destroyed with baked emitter cleanup");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static object CreateBakingSettings(BlobAssetStore blobAssetStore)
        {
            Assembly hybridAssembly = typeof(BakingSystem).Assembly;
            Type bakingSettingsType = hybridAssembly.GetType("Unity.Entities.BakingSettings", throwOnError: true);
            Type bakingUtilityType = hybridAssembly.GetType("Unity.Entities.BakingUtility", throwOnError: true);
            Type bakingFlagsType = bakingUtilityType.GetNestedType("BakingFlags", BindingFlags.Public);

            object flags = System.Enum.Parse(bakingFlagsType, "AssignName, AddEntityGUID");
            ConstructorInfo ctor = bakingSettingsType.GetConstructor(new[] { bakingFlagsType, typeof(BlobAssetStore) });
            return ctor.Invoke(new object[] { flags, blobAssetStore });
        }

        private static void InvokeBakeGameObjects(World world, object bakingSettings, params GameObject[] roots)
        {
            Assembly hybridAssembly = typeof(BakingSystem).Assembly;
            Type bakingUtilityType = hybridAssembly.GetType("Unity.Entities.BakingUtility", throwOnError: true);
            MethodInfo bakeGameObjects = bakingUtilityType.GetMethod("BakeGameObjects", BindingFlags.Static | BindingFlags.NonPublic);

            bakeGameObjects?.Invoke(null, new object[] { world, roots, bakingSettings });
        }

        private static Entity GetBakedEntity(BakingSystem bakingSystem, GameObject root)
        {
            MethodInfo getEntityMethod = typeof(BakingSystem).GetMethod(
                "GetEntity",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(GameObject) },
                modifiers: null);

            return (Entity)(getEntityMethod?.Invoke(bakingSystem, new object[] { root }) ?? Entity.Null);
        }

        private static void SetField(SpriteSheetDefinition definition, string fieldName, object value)
        {
            typeof(SpriteSheetDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }

        private static bool IsBlobAssetReferenceCreated<TComponent>(TComponent component, string fieldName)
            where TComponent : struct
        {
            object boxedComponent = component;
            object fieldValue = typeof(TComponent)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(boxedComponent);

            return (bool)(fieldValue?.GetType().GetProperty(nameof(BlobAssetReference<byte>.IsCreated))?.GetValue(fieldValue) ?? false);
        }
    }
}
