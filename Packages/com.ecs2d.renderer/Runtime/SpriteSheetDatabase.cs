using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECS2D.Rendering
{
    public static class SpriteSheetDatabase
    {
        private static readonly object SyncRoot = new object();
        private static SpriteSheetDefinition[] cachedDefinitions = Array.Empty<SpriteSheetDefinition>();
        private static Dictionary<int, SpriteSheetDefinition> cachedDefinitionsById = new Dictionary<int, SpriteSheetDefinition>();
        private static int cachedSignature = int.MinValue;
        private static bool isLoaded;

        internal static Func<SpriteSheetDefinition[]> DefinitionsLoader = DefaultLoadDefinitions;

        public static SpriteSheetDefinition[] Definitions => GetDefinitions();
        public static int DefinitionsSignature => GetDefinitionsSignature();

        public static SpriteSheetDefinition[] GetDefinitions()
        {
            EnsureLoaded();
            return cachedDefinitions;
        }

        public static bool TryGetDefinition(int sheetId, out SpriteSheetDefinition definition)
        {
            EnsureLoaded();
            return cachedDefinitionsById.TryGetValue(sheetId, out definition);
        }

        public static void RefreshCache()
        {
            lock (SyncRoot)
            {
                LoadDefinitionsIntoCache();
            }
        }

        public static int GetDefinitionsSignature()
        {
            EnsureLoaded();
            return cachedSignature;
        }

        internal static void ResetForTests()
        {
            lock (SyncRoot)
            {
                cachedDefinitions = Array.Empty<SpriteSheetDefinition>();
                cachedDefinitionsById = new Dictionary<int, SpriteSheetDefinition>();
                cachedSignature = int.MinValue;
                isLoaded = false;
            }
        }

        private static void EnsureLoaded()
        {
            if (isLoaded)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (!isLoaded)
                {
                    LoadDefinitionsIntoCache();
                }
            }
        }

        private static void LoadDefinitionsIntoCache()
        {
            var loadedDefinitions = DefinitionsLoader?.Invoke() ?? Array.Empty<SpriteSheetDefinition>();
            if (loadedDefinitions == null || loadedDefinitions.Length == 0)
            {
                cachedDefinitions = Array.Empty<SpriteSheetDefinition>();
                cachedDefinitionsById = new Dictionary<int, SpriteSheetDefinition>();
                cachedSignature = ComputeSignature(cachedDefinitions);
                isLoaded = true;
                return;
            }

            var definitions = new List<SpriteSheetDefinition>(loadedDefinitions.Length);
            var definitionsById = new Dictionary<int, SpriteSheetDefinition>(loadedDefinitions.Length);
            var seenSheetIds = new HashSet<int>();

            foreach (var definition in loadedDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (!seenSheetIds.Add(definition.SheetId))
                {
                    Debug.LogWarning($"Duplicate SpriteSheetDefinition id {definition.SheetId} found on '{definition.name}'.");
                }

                definitions.Add(definition);
                definitionsById[definition.SheetId] = definition;
            }

            definitions.Sort((left, right) => left.SheetId.CompareTo(right.SheetId));
            cachedDefinitions = definitions.ToArray();
            cachedDefinitionsById = definitionsById;
            cachedSignature = ComputeSignature(cachedDefinitions);
            isLoaded = true;
        }

        private static SpriteSheetDefinition[] DefaultLoadDefinitions()
        {
            var loadedDefinitions = Resources.LoadAll<SpriteSheetDefinition>("SpriteSheets");
            return loadedDefinitions == null || loadedDefinitions.Length == 0
                ? Array.Empty<SpriteSheetDefinition>()
                : loadedDefinitions;
        }

        private static int ComputeSignature(SpriteSheetDefinition[] definitions)
        {
            var hash = new HashCode();
            hash.Add(definitions.Length);

            for (int i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    hash.Add(0);
                    continue;
                }

                hash.Add(definition.SheetId);
                hash.Add(definition.FrameCount);
                hash.Add(definition.BaseMaterial != null ? definition.BaseMaterial.GetInstanceID() : 0);
                hash.Add(definition.Texture != null ? definition.Texture.GetInstanceID() : 0);
                hash.Add(definition.WorldBounds.center);
                hash.Add(definition.WorldBounds.size);
                hash.Add(definition.InitialCapacity);
                hash.Add(definition.CapacityStep);

                var frames = definition.Frames;
                hash.Add(frames.Length);
                for (int frameIndex = 0; frameIndex < frames.Length; frameIndex++)
                {
                    hash.Add(frames[frameIndex]);
                }
            }

            return hash.ToHashCode();
        }
    }
}
