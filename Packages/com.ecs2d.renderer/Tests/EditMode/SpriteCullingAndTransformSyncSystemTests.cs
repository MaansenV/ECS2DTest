using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteCullingAndTransformSyncSystemTests
    {
        [Test]
        public void SpriteTransformSyncSystem_UpdatesSpriteDataFromLocalToWorld()
        {
            using var world = new World("SpriteTransformSyncSystemTests");
            var entityManager = world.EntityManager;
            var localToWorldSystem = world.GetOrCreateSystem<LocalToWorldSystem>();
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateTransformDrivenSprite(world);

            entityManager.SetComponentData(
                entity,
                LocalTransform.FromPositionRotationScale(
                    new float3(2f, 3f, 4f),
                    quaternion.RotateZ(math.radians(45f)),
                    2.5f));

            localToWorldSystem.Update(world.Unmanaged);
            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.y, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.z, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(math.radians(45f)).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(2.5f).Within(0.0001f));
        }

        [Test]
        public void SpriteTransformSyncSystem_PreservesFlipXFromNegativeXScale()
        {
            using var world = new World("SpriteTransformSyncSystemTests");
            var entityManager = world.EntityManager;
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateLocalToWorldDrivenSprite(world);

            float4x4 trs = float4x4.TRS(
                new float3(2f, 3f, 0f),
                quaternion.RotateZ(0f),
                new float3(-2.5f, 2.5f, 1f));
            entityManager.SetComponentData(entity, new LocalToWorld { Value = trs });

            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.y, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(spriteData.FlipX, Is.EqualTo(1));
            Assert.That(spriteData.FlipY, Is.EqualTo(0));
        }

        [Test]
        public void SpriteTransformSyncSystem_PreservesFlipXFromNegativeYScale()
        {
            using var world = new World("SpriteTransformSyncSystemTests");
            var entityManager = world.EntityManager;
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateLocalToWorldDrivenSprite(world);

            float4x4 trs = float4x4.TRS(
                new float3(0f, 0f, 0f),
                quaternion.RotateZ(0f),
                new float3(2.5f, -2.5f, 1f));
            entityManager.SetComponentData(entity, new LocalToWorld { Value = trs });

            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(math.PI).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(spriteData.FlipX, Is.EqualTo(1));
            Assert.That(spriteData.FlipY, Is.EqualTo(0));
        }

        [Test]
        public void SpriteTransformSyncSystem_NoFlipWithDoubleNegativeScale()
        {
            using var world = new World("SpriteTransformSyncSystemTests");
            var entityManager = world.EntityManager;
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateLocalToWorldDrivenSprite(world);

            float4x4 trs = float4x4.TRS(
                new float3(0f, 0f, 0f),
                quaternion.RotateZ(0f),
                new float3(-2.5f, -2.5f, 1f));
            entityManager.SetComponentData(entity, new LocalToWorld { Value = trs });

            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(math.PI).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(2.5f).Within(0.0001f));
            Assert.That(spriteData.FlipX, Is.EqualTo(0));
            Assert.That(spriteData.FlipY, Is.EqualTo(0));
        }

        [Test]
        public void SpriteTransformSyncSystem_PreservesFlipXWithRotationAndNegativeScale()
        {
            using var world = new World("SpriteTransformSyncSystemTests");
            var entityManager = world.EntityManager;
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateLocalToWorldDrivenSprite(world);

            float4x4 trs = float4x4.TRS(
                new float3(1f, 2f, 0f),
                quaternion.RotateZ(math.radians(45f)),
                new float3(-3f, 3f, 1f));
            entityManager.SetComponentData(entity, new LocalToWorld { Value = trs });

            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(math.radians(45f)).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(3f).Within(0.0001f));
            Assert.That(spriteData.FlipX, Is.EqualTo(1));
            Assert.That(spriteData.FlipY, Is.EqualTo(0));
        }

        [Test]
        public void SpriteTransformSyncSystem_UpdatesDisabledCullEntitiesToo()
        {
            using var world = new World("SpriteTransformSyncDisabledTests");
            var entityManager = world.EntityManager;
            var localToWorldSystem = world.GetOrCreateSystem<LocalToWorldSystem>();
            var syncSystem = world.CreateSystem<SpriteTransformSyncSystem>();
            Entity entity = CreateTransformDrivenSprite(world);

            entityManager.SetComponentEnabled<SpriteCullState>(entity, false);
            entityManager.SetComponentData(
                entity,
                LocalTransform.FromPositionRotationScale(
                    new float3(-4f, 1.5f, 0f),
                    quaternion.RotateZ(math.radians(-30f)),
                    1.75f));

            localToWorldSystem.Update(world.Unmanaged);
            syncSystem.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            SpriteData spriteData = entityManager.GetComponentData<SpriteData>(entity);
            Assert.That(spriteData.TranslationAndRotation.x, Is.EqualTo(-4f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.y, Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(spriteData.TranslationAndRotation.w, Is.EqualTo(math.radians(-30f)).Within(0.0001f));
            Assert.That(spriteData.Scale, Is.EqualTo(1.75f).Within(0.0001f));
            Assert.IsFalse(entityManager.IsComponentEnabled<SpriteCullState>(entity));
        }

        [Test]
        public void SpriteCullingSystem_DisablesOutsideSprites()
        {
            using var mainCameraScope = MainCameraScope.CreateOrthographic(new Vector3(0f, 0f, -10f), orthographicSize: 5f, aspect: 1f);
            using var world = new World("SpriteCullingOutsideTests");
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity entity = CreateSpriteWithCullState(world, new float2(6.1f, 0f), 1f);

            system.Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();

            Assert.IsFalse(world.EntityManager.IsComponentEnabled<SpriteCullState>(entity));
        }

        [Test]
        public void SpriteCullingSystem_EnablesInsideSprites()
        {
            using var mainCameraScope = MainCameraScope.CreateOrthographic(new Vector3(0f, 0f, -10f), orthographicSize: 5f, aspect: 1f);
            using var world = new World("SpriteCullingInsideTests");
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity entity = CreateSpriteWithCullState(world, new float2(0f, 0f), 1f);

            world.EntityManager.SetComponentEnabled<SpriteCullState>(entity, false);
            system.Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(world.EntityManager.IsComponentEnabled<SpriteCullState>(entity));
        }

        [Test]
        public void SpriteCullingSystem_ReenablesReturningSprites()
        {
            using var mainCameraScope = MainCameraScope.CreateOrthographic(new Vector3(0f, 0f, -10f), orthographicSize: 5f, aspect: 1f);
            using var world = new World("SpriteCullingReentryTests");
            var entityManager = world.EntityManager;
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity entity = CreateSpriteWithCullState(world, new float2(7f, 0f), 1f);

            system.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();
            Assert.IsFalse(entityManager.IsComponentEnabled<SpriteCullState>(entity));

            LocalToWorld localToWorld = entityManager.GetComponentData<LocalToWorld>(entity);
            localToWorld.Value = float4x4.TRS(new float3(0f, 0f, 0f), quaternion.identity, new float3(1f, 1f, 1f));
            entityManager.SetComponentData(entity, localToWorld);

            system.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(entity));
        }

        [Test]
        public void SpriteCullingSystem_FallsBackToVisibleWithoutCamera()
        {
            using var mainCameraScope = MainCameraScope.WithoutMainCamera();
            using var world = new World("SpriteCullingFallbackTests");
            var entityManager = world.EntityManager;
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity first = CreateSpriteWithCullState(world, new float2(100f, 100f), 1f);
            Entity second = CreateSpriteWithCullState(world, new float2(-100f, -100f), 1f);

            entityManager.SetComponentEnabled<SpriteCullState>(first, false);
            entityManager.SetComponentEnabled<SpriteCullState>(second, false);

            system.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(first));
            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(second));
        }

        [Test]
        public void SpriteCullingSystem_LeavesSpritesVisibleWhenDisabledInSettings()
        {
            using var mainCameraScope = MainCameraScope.CreateOrthographic(new Vector3(0f, 0f, -10f), orthographicSize: 5f, aspect: 1f);
            using var world = new World("SpriteCullingSettingsDisabledTests");
            var entityManager = world.EntityManager;
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity entity = CreateSpriteWithCullState(world, new float2(100f, 100f), 1f);
            Entity settingsEntity = entityManager.CreateEntity(typeof(SpriteCullingSettings));

            entityManager.SetComponentData(settingsEntity, new SpriteCullingSettings
            {
                Enabled = 0
            });
            entityManager.SetComponentEnabled<SpriteCullState>(entity, false);

            system.Update(world.Unmanaged);
            entityManager.CompleteAllTrackedJobs();

            Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(entity));
        }

        [Test]
        public void SpriteCullingSystem_LeavesSpritesVisibleWhenDisabledByRuntimeOverride()
        {
            using var mainCameraScope = MainCameraScope.CreateOrthographic(new Vector3(0f, 0f, -10f), orthographicSize: 5f, aspect: 1f);
            using var world = new World("SpriteCullingRuntimeOverrideDisabledTests");
            var entityManager = world.EntityManager;
            var system = world.CreateSystem<SpriteCullingSystem>();
            Entity entity = CreateSpriteWithCullState(world, new float2(100f, 100f), 1f);

            SpriteCullingRuntime.SetOverride(false);

            try
            {
                entityManager.SetComponentEnabled<SpriteCullState>(entity, false);
                system.Update(world.Unmanaged);
                entityManager.CompleteAllTrackedJobs();

                Assert.IsTrue(entityManager.IsComponentEnabled<SpriteCullState>(entity));
            }
            finally
            {
                SpriteCullingRuntime.ClearOverride();
            }
        }

        [Test]
        public void SpriteSystem_CountsOnlyEnabledSprites()
        {
            using var world = new World("SpriteSystemCountTests");
            var entityManager = world.EntityManager;
            var spriteSystem = world.GetOrCreateSystemManaged<SpriteSystem>();

            Entity first = CreateSpriteWithCullState(world, new float2(0f, 0f), 1f);
            Entity second = CreateSpriteWithCullState(world, new float2(1f, 0f), 1f);
            Entity third = CreateSpriteWithCullState(world, new float2(2f, 0f), 1f);

            entityManager.SetComponentEnabled<SpriteCullState>(second, false);

            EntityQuery allSpriteQuery = GetPrivateQuery(spriteSystem, "allSpriteQuery");
            Assert.AreEqual(2, allSpriteQuery.CalculateEntityCount());

            entityManager.SetComponentEnabled<SpriteCullState>(first, false);
            entityManager.SetComponentEnabled<SpriteCullState>(third, false);

            Assert.AreEqual(0, allSpriteQuery.CalculateEntityCount());
        }

        [Test]
        public void SpriteSystem_BaseIndexArrayMatchesEnabledSubset()
        {
            using var world = new World("SpriteSystemBaseIndexTests");
            var entityManager = world.EntityManager;
            var spriteSystem = world.GetOrCreateSystemManaged<SpriteSystem>();
            EntityArchetype archetype = entityManager.CreateArchetype(typeof(SpriteData), typeof(SpriteCullState), typeof(SpriteSheetRenderKey));

            for (int i = 0; i < 512; i++)
            {
                Entity entity = entityManager.CreateEntity(archetype);
                entityManager.SetComponentData(entity, new SpriteData
                {
                    TranslationAndRotation = new float4(i, 0f, 0f, 0f),
                    Scale = 1f,
                    Color = new float4(1f),
                    SpriteFrameIndex = i,
                    SpriteSheetId = 7
                });
                entityManager.SetSharedComponent(entity, new SpriteSheetRenderKey { SheetId = 7 });
                entityManager.SetComponentEnabled<SpriteCullState>(entity, (i % 3) != 0);
            }

            EntityQuery filteredSpriteQuery = GetPrivateQuery(spriteSystem, "filteredSpriteQuery");
            filteredSpriteQuery.SetSharedComponentFilter(new SpriteSheetRenderKey { SheetId = 7 });

            NativeArray<int> baseIndices = filteredSpriteQuery.CalculateBaseEntityIndexArrayAsync(
                Allocator.TempJob,
                default(JobHandle),
                out JobHandle baseIndexHandle);
            baseIndexHandle.Complete();

            using NativeArray<ArchetypeChunk> chunks = filteredSpriteQuery.ToArchetypeChunkArray(Allocator.Temp);
            ComponentTypeHandle<SpriteCullState> cullStateType = entityManager.GetComponentTypeHandle<SpriteCullState>(true);

            int expectedBaseIndex = 0;
            Assert.AreEqual(chunks.Length, baseIndices.Length);

            for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                ArchetypeChunk chunk = chunks[chunkIndex];
                EnabledMask enabledMask = chunk.GetEnabledMask(ref cullStateType);

                Assert.AreEqual(expectedBaseIndex, baseIndices[chunkIndex]);

                int enabledCount = 0;
                for (int i = 0; i < chunk.Count; i++)
                {
                    if (enabledMask.GetEnabledRefRO<SpriteCullState>(i).ValueRO)
                    {
                        enabledCount++;
                    }
                }

                expectedBaseIndex += enabledCount;
            }

            Assert.AreEqual(filteredSpriteQuery.CalculateEntityCount(), expectedBaseIndex);

            filteredSpriteQuery.ResetFilter();
            baseIndices.Dispose();
        }

        private static Entity CreateTransformDrivenSprite(World world)
        {
            Entity entity = world.EntityManager.CreateEntity(
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(SpriteData),
                typeof(SpriteCullState));

            world.EntityManager.SetComponentData(entity, LocalTransform.Identity);
            world.EntityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.identity
            });
            world.EntityManager.SetComponentData(entity, new SpriteData
            {
                TranslationAndRotation = float4.zero,
                Scale = 1f,
                Color = new float4(1f),
                SpriteFrameIndex = 0,
                SpriteSheetId = 7
            });

            return entity;
        }

        private static Entity CreateLocalToWorldDrivenSprite(World world)
        {
            Entity entity = world.EntityManager.CreateEntity(
                typeof(LocalToWorld),
                typeof(SpriteData),
                typeof(SpriteCullState));

            world.EntityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.identity
            });
            world.EntityManager.SetComponentData(entity, new SpriteData
            {
                TranslationAndRotation = float4.zero,
                Scale = 1f,
                Color = new float4(1f),
                SpriteFrameIndex = 0,
                SpriteSheetId = 7
            });

            return entity;
        }

        private static Entity CreateSpriteWithCullState(World world, float2 position, float scale)
        {
            Entity entity = world.EntityManager.CreateEntity(typeof(LocalToWorld), typeof(SpriteData), typeof(SpriteCullState));
            world.EntityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = float4x4.TRS(new float3(position.x, position.y, 0f), quaternion.identity, new float3(scale, scale, 1f))
            });
            world.EntityManager.SetComponentData(entity, new SpriteData
            {
                TranslationAndRotation = new float4(position.x, position.y, 0f, 0f),
                Scale = scale,
                Color = new float4(1f),
                SpriteFrameIndex = 0,
                SpriteSheetId = 7
            });

            return entity;
        }

        private static EntityQuery GetPrivateQuery(SpriteSystem spriteSystem, string fieldName)
        {
            FieldInfo field = typeof(SpriteSystem).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected SpriteSystem to contain private field '{fieldName}'.");
            return (EntityQuery)field.GetValue(spriteSystem);
        }

        private sealed class MainCameraScope : IDisposable
        {
            private readonly GameObject[] previousMainCameraObjects;
            private readonly GameObject createdCameraObject;

            private MainCameraScope(GameObject[] previousMainCameraObjects, GameObject createdCameraObject)
            {
                this.previousMainCameraObjects = previousMainCameraObjects;
                this.createdCameraObject = createdCameraObject;
            }

            public static MainCameraScope CreateOrthographic(Vector3 position, float orthographicSize, float aspect)
            {
                var previousMainCameraObjects = GameObject.FindGameObjectsWithTag("MainCamera");
                for (int i = 0; i < previousMainCameraObjects.Length; i++)
                {
                    previousMainCameraObjects[i].tag = "Untagged";
                }

                var cameraObject = new GameObject("SpriteCullingSystemTests.MainCamera");
                cameraObject.tag = "MainCamera";

                var camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = orthographicSize;
                camera.aspect = aspect;
                camera.transform.position = position;

                return new MainCameraScope(previousMainCameraObjects, cameraObject);
            }

            public static MainCameraScope WithoutMainCamera()
            {
                var previousMainCameraObjects = GameObject.FindGameObjectsWithTag("MainCamera");
                for (int i = 0; i < previousMainCameraObjects.Length; i++)
                {
                    previousMainCameraObjects[i].tag = "Untagged";
                }

                return new MainCameraScope(previousMainCameraObjects, createdCameraObject: null);
            }

            public void Dispose()
            {
                if (createdCameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdCameraObject);
                }

                for (int i = 0; i < previousMainCameraObjects.Length; i++)
                {
                    if (previousMainCameraObjects[i] != null)
                    {
                        previousMainCameraObjects[i].tag = "MainCamera";
                    }
                }
            }
        }
    }
}
