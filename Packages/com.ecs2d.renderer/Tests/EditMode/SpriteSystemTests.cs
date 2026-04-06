using System;
using System.Reflection;
using NUnit.Framework;
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

                object drawResult = renderGroupType.GetMethod("Draw")?.Invoke(renderGroup, new object[] { mesh });
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

        private static void SetField(SpriteSheetDefinition definition, string fieldName, object value)
        {
            typeof(SpriteSheetDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }
    }
}
