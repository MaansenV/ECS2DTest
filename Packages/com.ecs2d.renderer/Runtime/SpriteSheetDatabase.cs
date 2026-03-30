using System;
using System.Collections.Generic;
using UnityEngine;

namespace ECS2D.Rendering
{
    public static class SpriteSheetDatabase
    {
        private static SpriteSheetDefinition[] cachedDefinitions;

        public static SpriteSheetDefinition[] Definitions => GetDefinitions();

        public static SpriteSheetDefinition[] GetDefinitions()
        {
            if (cachedDefinitions == null)
            {
                LoadDefinitions();
            }

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
        }

        private static void LoadDefinitions()
        {
            var loadedDefinitions = Resources.LoadAll<SpriteSheetDefinition>("SpriteSheets");

            if (loadedDefinitions == null || loadedDefinitions.Length == 0)
            {
                cachedDefinitions = Array.Empty<SpriteSheetDefinition>();
                return;
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
            cachedDefinitions = definitions.ToArray();
        }
    }
}
