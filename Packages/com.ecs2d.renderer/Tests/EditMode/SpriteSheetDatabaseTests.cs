using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteSheetDatabaseTests
    {
        private readonly List<SpriteSheetDefinition> _createdDefinitions = new List<SpriteSheetDefinition>();
        private Func<SpriteSheetDefinition[]> _originalLoader;

        [SetUp]
        public void SetUp()
        {
            _originalLoader = SpriteSheetDatabase.DefinitionsLoader;
            SpriteSheetDatabase.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            SpriteSheetDatabase.DefinitionsLoader = _originalLoader;
            SpriteSheetDatabase.ResetForTests();

            foreach (var definition in _createdDefinitions)
            {
                if (definition != null)
                {
                    UnityEngine.Object.DestroyImmediate(definition);
                }
            }

            _createdDefinitions.Clear();
        }

        [Test]
        public void GetDefinitionsAndSignature_ReuseCachedLoadUntilRefresh()
        {
            int loadCount = 0;
            var firstDefinition = CreateDefinition(1, new[]
            {
                new Vector4(1f, 1f, 0f, 0f)
            });
            var secondDefinition = CreateDefinition(2, new[]
            {
                new Vector4(0.5f, 0.5f, 0f, 0f),
                new Vector4(0.5f, 0.5f, 0.5f, 0f)
            });

            SpriteSheetDatabase.DefinitionsLoader = () =>
            {
                loadCount++;
                return loadCount == 1
                    ? new[] { firstDefinition }
                    : new[] { secondDefinition };
            };

            var cachedDefinitions = SpriteSheetDatabase.GetDefinitions();
            int cachedSignature = SpriteSheetDatabase.GetDefinitionsSignature();
            int cachedSignatureAgain = SpriteSheetDatabase.GetDefinitionsSignature();

            Assert.AreEqual(1, loadCount);
            Assert.AreSame(firstDefinition, cachedDefinitions[0]);
            Assert.AreEqual(cachedSignature, cachedSignatureAgain);

            SpriteSheetDatabase.RefreshCache();

            var refreshedDefinitions = SpriteSheetDatabase.GetDefinitions();
            int refreshedSignature = SpriteSheetDatabase.GetDefinitionsSignature();

            Assert.AreEqual(2, loadCount);
            Assert.AreSame(secondDefinition, refreshedDefinitions[0]);
            Assert.AreNotEqual(cachedSignature, refreshedSignature);
            Assert.IsTrue(SpriteSheetDatabase.TryGetDefinition(2, out var foundDefinition));
            Assert.AreSame(secondDefinition, foundDefinition);
        }

        private SpriteSheetDefinition CreateDefinition(int sheetId, Vector4[] frames)
        {
            var definition = ScriptableObject.CreateInstance<SpriteSheetDefinition>();
            _createdDefinitions.Add(definition);

            SetField(definition, "sheetId", sheetId);
            SetField(definition, "autoGenerateGridFrames", false);
            SetField(definition, "frames", frames);
            SetField(definition, "initialCapacity", 8);
            SetField(definition, "capacityStep", 8);

            return definition;
        }

        private static void SetField(SpriteSheetDefinition definition, string fieldName, object value)
        {
            typeof(SpriteSheetDefinition)
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(definition, value);
        }
    }
}
