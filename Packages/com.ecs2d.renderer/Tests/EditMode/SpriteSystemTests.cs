using System;
using System.Reflection;
using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteSystemTests
    {
        [Test]
        public void SpriteRenderGroup_DrawReturnsFalseAfterDisposeInsteadOfTouchingDestroyedMaterial()
        {
            var definition = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            var material = new Material(Shader.Find("Sprites/Default"));
            var mesh = new Mesh();

            object renderGroup = null;

            try
            {
                SetField(definition, "baseMaterial", material);
                SetField(definition, "autoGenerateGridFrames", false);
                SetField(definition, "frames", new[] { new Vector4(1f, 1f, 0f, 0f) });
                SetField(definition, "initialCapacity", 1);
                SetField(definition, "capacityStep", 1);

                Type renderGroupType = typeof(SpriteSystem).GetNestedType("SpriteRenderGroup", BindingFlags.NonPublic);
                Assert.IsNotNull(renderGroupType);

                renderGroup = Activator.CreateInstance(
                    renderGroupType,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null,
                    args: new object[] { definition },
                    culture: null);
                Assert.IsNotNull(renderGroup);

                renderGroupType.GetMethod("Dispose")?.Invoke(renderGroup, null);
                renderGroupType.GetField("WriteIndex")?.SetValue(renderGroup, 1);

                object drawResult = renderGroupType.GetMethod("Draw")?.Invoke(renderGroup, new object[] { mesh, true, true });
                Assert.AreEqual(false, drawResult);
            }
            finally
            {
                if (renderGroup is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                UnityEngine.Object.DestroyImmediate(mesh);
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SpriteSystem_ShadowEligibleQuery_ExcludesParticleRuntimeEntities()
        {
            using var world = new World("SpriteSystemShadowFilterTests");
            var entityManager = world.EntityManager;
            world.CreateSystem<SpriteSystem>();

            Entity normalSprite = entityManager.CreateEntity(typeof(SpriteData), typeof(SpriteCullState), typeof(SpriteSheetRenderKey));
            Entity particleSprite = entityManager.CreateEntity(typeof(SpriteData), typeof(SpriteCullState), typeof(SpriteSheetRenderKey), typeof(ParticleRuntime));

            var spriteData = new SpriteData
            {
                TranslationAndRotation = float4.zero,
                BaseScale = 1f,
                BaseScaleXY = new float2(1f, 1f),
                RotationOffsetRadians = 0f,
                Scale = 1f,
                ScaleXY = float2.zero,
                Color = new float4(1f, 1f, 1f, 1f),
                RenderDepth = 0f,
                SpriteFrameIndex = 0,
                SpriteSheetId = 7,
                SortingLayer = 0,
                FlipX = 0,
                FlipY = 0
            };

            entityManager.SetComponentData(normalSprite, spriteData);
            entityManager.SetComponentData(particleSprite, spriteData);
            entityManager.SetComponentData(particleSprite, new ParticleRuntime());

            var renderKey = new SpriteSheetRenderKey { SheetId = 7 };
            entityManager.SetSharedComponent(normalSprite, renderKey);
            entityManager.SetSharedComponent(particleSprite, renderKey);

            var system = world.GetExistingSystemManaged<SpriteSystem>();
            Type systemType = typeof(SpriteSystem);

            EntityQuery allQuery = (EntityQuery)systemType
                .GetField("filteredSpriteQuery", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(system);
            EntityQuery shadowEligibleQuery = (EntityQuery)systemType
                .GetField("shadowEligibleSpriteQuery", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(system);

            Assert.IsNotNull(allQuery);
            Assert.IsNotNull(shadowEligibleQuery);

            allQuery.SetSharedComponentFilter(renderKey);
            shadowEligibleQuery.SetSharedComponentFilter(renderKey);

            Assert.AreEqual(2, allQuery.CalculateEntityCount());
            Assert.AreEqual(1, shadowEligibleQuery.CalculateEntityCount());

            allQuery.ResetFilter();
            shadowEligibleQuery.ResetFilter();
        }

        private static void SetField(SpriteSheetDefinition definition, string fieldName, object value)
        {
            typeof(SpriteSheetDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }
    }
}
