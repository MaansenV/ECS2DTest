using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECS2D.Rendering
{
    public static class SpriteSheetDatabase
    {
        private static SpriteSheetDefinition[] cachedDefinitions;
        private static int cachedSignature = int.MinValue;

        public static SpriteSheetDefinition[] Definitions => GetDefinitions();
        public static int DefinitionsSignature => GetDefinitionsSignature();

        public static SpriteSheetDefinition[] GetDefinitions()
        {
            RefreshDefinitionsIfNeeded();

            return cachedDefinitions;
        }

        public static bool TryGetDefinition(int sheetId, out SpriteSheetDefinition definition)
        {
            var definitions = GetDefinitions();

            for (int i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].SheetId == sheetId)
                {
                    definition = definitions[i];
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static void RefreshCache()
        {
            cachedDefinitions = null;
            cachedSignature = int.MinValue;
        }

        public static int GetDefinitionsSignature()
        {
            RefreshDefinitionsIfNeeded();
            return cachedSignature;
        }

        private static void RefreshDefinitionsIfNeeded()
        {
            var loadedDefinitions = LoadDefinitions();
            int newSignature = ComputeSignature(loadedDefinitions);

            if (cachedDefinitions == null || cachedSignature != newSignature)
            {
                cachedDefinitions = loadedDefinitions;
                cachedSignature = newSignature;
            }
        }

        private static SpriteSheetDefinition[] LoadDefinitions()
        {
            var loadedDefinitions = Resources.LoadAll<SpriteSheetDefinition>("SpriteSheets");

            if (loadedDefinitions == null || loadedDefinitions.Length == 0)
            {
                return Array.Empty<SpriteSheetDefinition>();
            }

            var definitions = new List<SpriteSheetDefinition>(loadedDefinitions.Length);
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
            }

            definitions.Sort((left, right) => left.SheetId.CompareTo(right.SheetId));
            return definitions.ToArray();
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
