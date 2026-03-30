using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using UnityEngine;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteAuthoringBakeTests
    {
        [Test]
        public void SpriteDataAuthoring_BakesSharedRenderKeyForInitialSheet()
        {
            using var world = new World("SpriteAuthoringBakeTests");
            using var blobAssetStore = new BlobAssetStore();

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var root = new GameObject("SpriteAuthoringBakeTests");

            try
            {
                SetField(sheet, "sheetId", 42);
                SetField(sheet, "autoGenerateGridFrames", false);
                SetField(sheet, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });

                var authoring = root.AddComponent<SpriteDataAuthoring>();
                authoring.SpriteSheet = sheet;

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, root);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                var bakedEntity = GetBakedEntity(bakingSystem, root);
                var renderKey = world.EntityManager.GetSharedComponent<SpriteSheetRenderKey>(bakedEntity);

                Assert.AreEqual(42, renderKey.SheetId);
                Assert.IsTrue(world.EntityManager.HasComponent<SpriteCullState>(bakedEntity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        [Test]
        public void SpriteAnimationAuthoring_BakesResolvedStartClipIndex()
        {
            using var world = new World("SpriteAuthoringBakeTests");
            using var blobAssetStore = new BlobAssetStore();

            var sheet = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var animationSet = ScriptableObject.CreateInstance<SpriteAnimationSetDefinition>();
            var root = new GameObject("SpriteAnimationAuthoringBakeTests");

            try
            {
                SetField(sheet, "sheetId", 7);
                SetField(sheet, "columns", 4);
                SetField(sheet, "rows", 2);
                SetField(sheet, "autoGenerateGridFrames", true);

                animationSet.SpriteSheet = sheet;
                animationSet.Clips.Add(new SpriteAnimationClip
                {
                    Name = "Idle",
                    Row = 0,
                    StartColumn = 0,
                    FrameCount = 2,
                    FrameRate = 2f,
                    Loop = true
                });
                animationSet.Clips.Add(new SpriteAnimationClip
                {
                    Name = "Run",
                    Row = 1,
                    StartColumn = 1,
                    FrameCount = 3,
                    FrameRate = 1f,
                    Loop = true
                });

                var authoring = root.AddComponent<SpriteAnimationAuthoring>();
                authoring.AnimationSet = animationSet;
                authoring.StartAnimation = "Run";
                authoring.PlayOnStart = true;
                authoring.PlaybackSpeed = 1.5f;

                object bakingSettings = CreateBakingSettings(blobAssetStore);
                InvokeBakeGameObjects(world, bakingSettings, root);

                var bakingSystem = world.GetOrCreateSystemManaged<BakingSystem>();
                var bakedEntity = GetBakedEntity(bakingSystem, root);
                var state = world.EntityManager.GetComponentData<SpriteAnimationState>(bakedEntity);
                var spriteData = world.EntityManager.GetComponentData<SpriteData>(bakedEntity);

                Assert.AreEqual(1, state.CurrentClipIndex);
                Assert.AreEqual(0, state.CurrentFrameIndex);
                Assert.AreEqual(1.5f, state.PlaybackSpeed, 0.0001f);
                Assert.AreEqual(1, state.Playing);
                Assert.AreEqual(5, spriteData.SpriteFrameIndex);
                Assert.IsTrue(world.EntityManager.HasComponent<SpriteCullState>(bakedEntity));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(animationSet);
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static object CreateBakingSettings(BlobAssetStore blobAssetStore)
        {
            Assembly hybridAssembly = typeof(BakingSystem).Assembly;
            Type bakingSettingsType = hybridAssembly.GetType("Unity.Entities.BakingSettings", throwOnError: true);
            Type bakingUtilityType = hybridAssembly.GetType("Unity.Entities.BakingUtility", throwOnError: true);
            Type bakingFlagsType = bakingUtilityType.GetNestedType("BakingFlags", BindingFlags.Public);

            object bakingSettings = Activator.CreateInstance(bakingSettingsType);
            object flags = Enum.Parse(bakingFlagsType, "AssignName, AddEntityGUID");

            bakingSettingsType.GetField("BakingFlags", BindingFlags.Instance | BindingFlags.Public)?.SetValue(bakingSettings, flags);
            bakingSettingsType.GetProperty("BlobAssetStore", BindingFlags.Instance | BindingFlags.Public)?.SetValue(bakingSettings, blobAssetStore);

            return bakingSettings;
        }

        private static void InvokeBakeGameObjects(World world, object bakingSettings, GameObject root)
        {
            Assembly hybridAssembly = typeof(BakingSystem).Assembly;
            Type bakingUtilityType = hybridAssembly.GetType("Unity.Entities.BakingUtility", throwOnError: true);
            MethodInfo bakeGameObjects = bakingUtilityType.GetMethod("BakeGameObjects", BindingFlags.Static | BindingFlags.NonPublic);

            bakeGameObjects?.Invoke(null, new object[] { world, new[] { root }, bakingSettings });
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
    }
}
