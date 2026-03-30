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
