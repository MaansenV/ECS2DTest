using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ECS2D.Rendering.Editor
{
    internal sealed class SpriteSheetEditorResolvedSource
    {
        public UnityEngine.Object SourceObject;
        public Texture2D Texture;
        public Sprite PreviewSprite;
        public IReadOnlyList<Sprite> Sprites = Array.Empty<Sprite>();
        public string BaseName = "SpriteSheet";
        public bool HasDetectedGrid;
        public int DetectedColumns = 1;
        public int DetectedRows = 1;
        public string DetectionMessage = string.Empty;
    }

    internal sealed class SpriteAnimationClipDraft
    {
        public string Name = string.Empty;
        public int Row;
        public int StartColumn;
        public int FrameCount = 1;
        public float FrameRate = 12f;
        public bool Loop = true;
        public bool PingPong;

        public SpriteAnimationClip ToClip()
        {
            return new SpriteAnimationClip
            {
                Name = Name ?? string.Empty,
                Row = Row,
                StartColumn = StartColumn,
                FrameCount = FrameCount,
                FrameRate = FrameRate,
                Loop = Loop,
                PingPong = PingPong
            };
        }

        public static SpriteAnimationClipDraft FromClip(SpriteAnimationClip clip)
        {
            return new SpriteAnimationClipDraft
            {
                Name = clip.Name,
                Row = clip.Row,
                StartColumn = clip.StartColumn,
                FrameCount = clip.FrameCount,
                FrameRate = clip.FrameRate,
                Loop = clip.Loop,
                PingPong = clip.PingPong
            };
        }
    }

    internal static class SpriteSheetEditorAssetUtility
    {
        internal const string DefaultOutputFolder = "Assets/Resources/SpriteSheets";
        private static readonly Regex TrailingNumberPattern = new Regex(@"^(.*?)(?:[_\s-]?)(\d+)$", RegexOptions.Compiled);

        public static List<SpriteSheetDefinition> FindAllSpriteSheets()
        {
            return AssetDatabase.FindAssets($"t:{nameof(SpriteSheetDefinition)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<SpriteSheetDefinition>(path))
                .Where(asset => asset != null)
                .OrderBy(asset => asset.SheetId)
                .ThenBy(asset => AssetDatabase.GetAssetPath(asset), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static List<SpriteAnimationSetDefinition> FindAllAnimationSets()
        {
            return AssetDatabase.FindAssets($"t:{nameof(SpriteAnimationSetDefinition)}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<SpriteAnimationSetDefinition>(path))
                .Where(asset => asset != null)
                .OrderBy(asset => AssetDatabase.GetAssetPath(asset), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static bool TryGetSheetById(int sheetId, out SpriteSheetDefinition definition)
        {
            definition = FindAllSpriteSheets().FirstOrDefault(sheet => sheet.SheetId == sheetId);
            return definition != null;
        }

        public static int SuggestNextSheetId()
        {
            int nextId = 1;
            var allSheets = FindAllSpriteSheets();
            for (int i = 0; i < allSheets.Count; i++)
            {
                nextId = Mathf.Max(nextId, allSheets[i].SheetId + 1);
            }

            return nextId;
        }

        public static List<SpriteAnimationSetDefinition> FindAnimationSetsForSheet(SpriteSheetDefinition sheet)
        {
            if (sheet == null)
            {
                return new List<SpriteAnimationSetDefinition>();
            }

            return FindAllAnimationSets()
                .Where(set => set.SpriteSheet == sheet)
                .OrderBy(set => AssetDatabase.GetAssetPath(set), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static string SuggestOutputFolder(UnityEngine.Object sourceObject)
        {
            if (sourceObject == null)
            {
                return DefaultOutputFolder;
            }

            string sourcePath = AssetDatabase.GetAssetPath(sourceObject);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return DefaultOutputFolder;
            }

            string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
            {
                return DefaultOutputFolder;
            }

            const string resourcesMarker = "/Resources/";
            int resourcesIndex = directory.IndexOf(resourcesMarker, StringComparison.OrdinalIgnoreCase);
            if (resourcesIndex >= 0)
            {
                string resourcesRoot = directory.Substring(0, resourcesIndex + resourcesMarker.Length - 1);
                return $"{resourcesRoot}/SpriteSheets";
            }

            return DefaultOutputFolder;
        }

        public static string SuggestAssetName(string baseName, string suffix)
        {
            string sanitized = SanitizeName(baseName);
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "SpriteSheet";
            }

            if (string.IsNullOrWhiteSpace(suffix))
            {
                return sanitized;
            }

            return sanitized.EndsWith(suffix, StringComparison.Ordinal)
                ? sanitized
                : sanitized + suffix;
        }

        public static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var invalidCharacters = Path.GetInvalidFileNameChars();
            var sanitized = new string(value.Trim().Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
            return sanitized.Replace(' ', '_');
        }

        public static bool IsRecommendedAssetName(string assetName, string expectedSuffix)
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return false;
            }

            return assetName.EndsWith(expectedSuffix, StringComparison.Ordinal);
        }

        public static bool IsValidRuntimeOutputFolder(string folderPath, out string validationMessage)
        {
            validationMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                validationMessage = "Output folder is required.";
                return false;
            }

            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) && !string.Equals(normalized, "Assets", StringComparison.OrdinalIgnoreCase))
            {
                validationMessage = "Output folder must be inside the Unity project under Assets/.";
                return false;
            }

            bool isValidResourcesSpriteSheetsPath = normalized.EndsWith("/Resources/SpriteSheets", StringComparison.OrdinalIgnoreCase)
                || normalized.IndexOf("/Resources/SpriteSheets/", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!isValidResourcesSpriteSheetsPath)
            {
                validationMessage = "Output folder must live under a Resources/SpriteSheets path so SpriteSheetDatabase can load it at runtime.";
                return false;
            }

            return true;
        }

        public static bool TryProjectRelativePath(string absolutePath, out string projectRelativePath)
        {
            projectRelativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(absolutePath))
            {
                return false;
            }

            string fullProjectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
                .Replace('\\', '/').TrimEnd('/');
            string fullSelectedPath = Path.GetFullPath(absolutePath)
                .Replace('\\', '/').TrimEnd('/');

            if (!fullSelectedPath.StartsWith(fullProjectPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string relative = fullSelectedPath.Substring(fullProjectPath.Length).TrimStart('/');
            projectRelativePath = relative;
            return !string.IsNullOrWhiteSpace(projectRelativePath);
        }

        public static void EnsureFolderExists(string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').Trim('/');
            if (string.IsNullOrWhiteSpace(normalized) || AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string[] segments = normalized.Split('/');
            string currentPath = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string nextPath = currentPath + "/" + segments[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, segments[i]);
                }

                currentPath = nextPath;
            }
        }

        public static SpriteSheetEditorResolvedSource ResolveSource(UnityEngine.Object sourceObject)
        {
            var resolved = new SpriteSheetEditorResolvedSource
            {
                SourceObject = sourceObject
            };

            if (sourceObject == null)
            {
                resolved.DetectionMessage = "No source selected. Configure the texture, grid, and clips manually.";
                return resolved;
            }

            if (sourceObject is SpriteAnimationSetDefinition animationSet)
            {
                sourceObject = animationSet.SpriteSheet;
            }

            if (sourceObject is SpriteSheetDefinition sheet)
            {
                resolved.Texture = sheet.Texture;
                resolved.BaseName = sheet.name;
                resolved.DetectedColumns = Mathf.Max(1, sheet.Columns);
                resolved.DetectedRows = Mathf.Max(1, sheet.Rows);
                resolved.HasDetectedGrid = sheet.AutoGenerateGridFrames;
                resolved.DetectionMessage = sheet.AutoGenerateGridFrames
                    ? "Loaded grid settings from the existing SpriteSheetDefinition."
                    : "Existing SpriteSheetDefinition uses manual frames. The wizard only supports grid-based animation clips in this MVP.";

                if (sheet.Texture != null)
                {
                    string texturePath = AssetDatabase.GetAssetPath(sheet.Texture);
                    resolved.Sprites = LoadSpritesAtPath(texturePath);
                    resolved.PreviewSprite = resolved.Sprites.FirstOrDefault();
                }

                return resolved;
            }

            if (sourceObject is Texture2D texture)
            {
                PopulateFromAssetPath(resolved, texture, AssetDatabase.GetAssetPath(texture));
                return resolved;
            }

            if (sourceObject is Sprite sprite)
            {
                PopulateFromAssetPath(resolved, sprite.texture, AssetDatabase.GetAssetPath(sprite));
                resolved.PreviewSprite = sprite;
                return resolved;
            }

            resolved.DetectionMessage = $"Unsupported source type '{sourceObject.GetType().Name}'. Use a Texture2D, Sprite, SpriteSheetDefinition, or SpriteAnimationSetDefinition.";
            return resolved;
        }

        public static List<SpriteAnimationClipDraft> BuildClipSuggestions(SpriteSheetEditorResolvedSource source, int columns, int rows, out string message)
        {
            var suggestions = new List<SpriteAnimationClipDraft>();
            message = string.Empty;

            if (source == null || source.Sprites == null || source.Sprites.Count == 0 || !source.HasDetectedGrid)
            {
                suggestions.Add(CreateDefaultClip(columns));
                message = "No grid-mapped sprites were available, so a default clip was created. You can edit it manually.";
                return suggestions;
            }

            if (!TryMapSpritesToGrid(source.Sprites, columns, rows, out var spriteGridInfo))
            {
                suggestions.Add(CreateDefaultClip(columns));
                message = "Could not map the selected sprites to the configured grid, so a default clip was created.";
                return suggestions;
            }

            var grouped = new Dictionary<string, List<(int Row, int Column, int Order, Sprite Sprite)>>(StringComparer.Ordinal);
            for (int i = 0; i < spriteGridInfo.Count; i++)
            {
                var mappedSprite = spriteGridInfo[i];
                string key = ExtractAnimationGroupName(mappedSprite.Sprite.name, out int order);
                if (!grouped.TryGetValue(key, out var group))
                {
                    group = new List<(int Row, int Column, int Order, Sprite Sprite)>();
                    grouped.Add(key, group);
                }

                group.Add((mappedSprite.Row, mappedSprite.Column, order, mappedSprite.Sprite));
            }

            foreach (var pair in grouped.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                var orderedFrames = pair.Value
                    .OrderBy(item => item.Order)
                    .ThenBy(item => item.Row)
                    .ThenBy(item => item.Column)
                    .ToList();

                int firstRow = orderedFrames[0].Row;
                bool singleRow = orderedFrames.All(item => item.Row == firstRow);
                if (!singleRow)
                {
                    continue;
                }

                int minColumn = orderedFrames.Min(item => item.Column);
                int maxColumn = orderedFrames.Max(item => item.Column);
                bool contiguous = (maxColumn - minColumn + 1) == orderedFrames.Count;
                if (!contiguous)
                {
                    continue;
                }

                suggestions.Add(new SpriteAnimationClipDraft
                {
                    Name = pair.Key,
                    Row = firstRow,
                    StartColumn = minColumn,
                    FrameCount = orderedFrames.Count,
                    FrameRate = 12f,
                    Loop = true,
                    PingPong = false
                });
            }

            if (suggestions.Count == 0)
            {
                suggestions.Add(CreateDefaultClip(columns));
                message = "No clip groups could be inferred from sprite names, so a default clip was created.";
                return suggestions;
            }

            message = $"Generated {suggestions.Count} clip suggestion(s) from sprite names.";
            return suggestions;
        }

        private static SpriteAnimationClipDraft CreateDefaultClip(int columns)
        {
            return new SpriteAnimationClipDraft
            {
                Name = "default",
                Row = 0,
                StartColumn = 0,
                FrameCount = Mathf.Max(1, columns),
                FrameRate = 12f,
                Loop = true,
                PingPong = false
            };
        }

        private static void PopulateFromAssetPath(SpriteSheetEditorResolvedSource resolved, Texture2D texture, string assetPath)
        {
            resolved.Texture = texture;
            resolved.BaseName = string.IsNullOrWhiteSpace(assetPath)
                ? (texture != null ? texture.name : "SpriteSheet")
                : Path.GetFileNameWithoutExtension(assetPath);
            resolved.Sprites = LoadSpritesAtPath(assetPath);
            resolved.PreviewSprite = resolved.Sprites.FirstOrDefault();

            if (TryDetectGrid(resolved.Sprites, out int columns, out int rows, out string detectionMessage))
            {
                resolved.HasDetectedGrid = true;
                resolved.DetectedColumns = columns;
                resolved.DetectedRows = rows;
                resolved.DetectionMessage = detectionMessage;
                return;
            }

            resolved.HasDetectedGrid = false;
            resolved.DetectedColumns = 1;
            resolved.DetectedRows = Mathf.Max(1, resolved.Sprites.Count);
            resolved.DetectionMessage = string.IsNullOrWhiteSpace(detectionMessage)
                ? "Could not infer a clean grid from the selected sprites. Enter columns and rows manually."
                : detectionMessage;
        }

        private static List<Sprite> LoadSpritesAtPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return new List<Sprite>();
            }

            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.y)
                .ThenBy(sprite => sprite.rect.x)
                .ThenBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToList();
        }

        private static bool TryDetectGrid(IReadOnlyList<Sprite> sprites, out int columns, out int rows, out string message)
        {
            columns = 1;
            rows = 1;
            message = string.Empty;

            if (sprites == null || sprites.Count == 0)
            {
                message = "No sliced sprites were found for the selected source.";
                return false;
            }

            if (sprites.Count == 1)
            {
                columns = 1;
                rows = 1;
                message = "Single sprite detected.";
                return true;
            }

            var widths = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.width)).Distinct().ToList();
            var heights = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.height)).Distinct().ToList();
            if (widths.Count != 1 || heights.Count != 1)
            {
                message = "Sprites do not share a consistent size, so the grid could not be inferred automatically.";
                return false;
            }

            var xPositions = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.xMin)).Distinct().OrderBy(value => value).ToList();
            var yPositions = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.yMin)).Distinct().OrderByDescending(value => value).ToList();
            columns = xPositions.Count;
            rows = yPositions.Count;

            if (columns * rows != sprites.Count)
            {
                message = "Sprite positions did not form a complete grid.";
                return false;
            }

            var coordinateSet = new HashSet<(int X, int Y)>();
            for (int i = 0; i < sprites.Count; i++)
            {
                var coordinate = (Mathf.RoundToInt(sprites[i].rect.xMin), Mathf.RoundToInt(sprites[i].rect.yMin));
                if (!coordinateSet.Add(coordinate))
                {
                    message = "Duplicate sprite positions were found while inferring the grid.";
                    return false;
                }
            }

            message = $"Detected a {columns}x{rows} grid from {sprites.Count} sprite(s).";
            return true;
        }

        private static string ExtractAnimationGroupName(string spriteName, out int order)
        {
            order = int.MaxValue;
            if (string.IsNullOrWhiteSpace(spriteName))
            {
                return "default";
            }

            Match match = TrailingNumberPattern.Match(spriteName);
            if (!match.Success)
            {
                return spriteName.Trim();
            }

            _ = int.TryParse(match.Groups[2].Value, out order);
            string baseName = match.Groups[1].Value.Trim();
            return string.IsNullOrWhiteSpace(baseName) ? spriteName.Trim() : baseName;
        }

        private static bool TryMapSpritesToGrid(IReadOnlyList<Sprite> sprites, int columns, int rows, out List<(Sprite Sprite, int Row, int Column)> result)
        {
            result = new List<(Sprite Sprite, int Row, int Column)>();
            if (sprites == null || sprites.Count == 0 || columns <= 0 || rows <= 0)
            {
                return false;
            }

            var xPositions = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.xMin)).Distinct().OrderBy(value => value).ToList();
            var yPositions = sprites.Select(sprite => Mathf.RoundToInt(sprite.rect.yMin)).Distinct().OrderByDescending(value => value).ToList();
            if (xPositions.Count != columns || yPositions.Count != rows)
            {
                return false;
            }

            var columnLookup = xPositions.Select((value, index) => new { value, index }).ToDictionary(item => item.value, item => item.index);
            var rowLookup = yPositions.Select((value, index) => new { value, index }).ToDictionary(item => item.value, item => item.index);
            for (int i = 0; i < sprites.Count; i++)
            {
                int x = Mathf.RoundToInt(sprites[i].rect.xMin);
                int y = Mathf.RoundToInt(sprites[i].rect.yMin);
                if (!columnLookup.TryGetValue(x, out int column) || !rowLookup.TryGetValue(y, out int row))
                {
                    return false;
                }

                result.Add((sprites[i], row, column));
            }

            return true;
        }
    }
}
