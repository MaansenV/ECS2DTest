using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ECS2D.Rendering.Editor
{
    public sealed class SpriteSheetOverviewWindow : EditorWindow
    {
        private sealed class OverviewRow
        {
            public SpriteSheetDefinition Sheet;
            public string Path;
            public int ClipCount;
        }

        private readonly List<OverviewRow> rows = new List<OverviewRow>();
        private Vector2 scrollPosition;
        private string idFilter = string.Empty;
        private string nameFilter = string.Empty;
        private string pathFilter = string.Empty;

        [MenuItem("Tools/ECS2D/Sprite Sheet Overview")]
        public static void OpenWindow()
        {
            var window = GetWindow<SpriteSheetOverviewWindow>();
            window.titleContent = new GUIContent("SpriteSheet Overview");
            window.minSize = new Vector2(720f, 360f);
            window.Show();
            window.RefreshRows();
        }

        [MenuItem("Assets/ECS2D/Open Sprite Sheet Overview", true)]
        private static bool ValidateOpenFromAssets()
        {
            return Selection.activeObject is Texture2D
                || Selection.activeObject is Sprite
                || Selection.activeObject is SpriteSheetDefinition
                || Selection.activeObject is SpriteAnimationSetDefinition;
        }

        [MenuItem("Assets/ECS2D/Open Sprite Sheet Overview")]
        private static void OpenFromAssets()
        {
            OpenWindow();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += RefreshRows;
            RefreshRows();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= RefreshRows;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawTable();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("SpriteSheet Overview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Read-only project-wide list of SpriteSheetDefinition assets.", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4f);
                using (new EditorGUILayout.HorizontalScope())
                {
                    idFilter = EditorGUILayout.TextField("ID", idFilter);
                    nameFilter = EditorGUILayout.TextField("Name", nameFilter);
                    pathFilter = EditorGUILayout.TextField("Path", pathFilter);

                    if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
                    {
                        RefreshRows();
                    }
                }
            }
        }

        private void DrawTable()
        {
            var filteredRows = rows.Where(MatchesFilters).ToList();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ID", GUILayout.Width(50f));
                GUILayout.Label("Name", GUILayout.Width(180f));
                GUILayout.Label("Path", GUILayout.Width(280f));
                GUILayout.Label("Sprites", GUILayout.Width(60f));
                GUILayout.Label("Animations", GUILayout.Width(80f));
                GUILayout.Label("Actions", GUILayout.Width(170f));
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (filteredRows.Count == 0)
            {
                EditorGUILayout.HelpBox("No SpriteSheetDefinition assets matched the current filters.", MessageType.Info);
            }

            for (int i = 0; i < filteredRows.Count; i++)
            {
                OverviewRow row = filteredRows[i];
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(row.Sheet.SheetId.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.LabelField(row.Sheet.name, GUILayout.Width(180f));
                    EditorGUILayout.SelectableLabel(row.Path, EditorStyles.label, GUILayout.Width(280f), GUILayout.Height(EditorGUIUtility.singleLineHeight));
                    EditorGUILayout.LabelField(row.Sheet.FrameCount.ToString(), GUILayout.Width(60f));
                    EditorGUILayout.LabelField(row.ClipCount.ToString(), GUILayout.Width(80f));

                    if (GUILayout.Button("Open", GUILayout.Width(50f)))
                    {
                        Selection.activeObject = row.Sheet;
                        EditorGUIUtility.PingObject(row.Sheet);
                    }

                    if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                    {
                        EditorGUIUtility.PingObject(row.Sheet);
                    }

                    if (GUILayout.Button("Copy ID", GUILayout.Width(60f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = row.Sheet.SheetId.ToString();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool MatchesFilters(OverviewRow row)
        {
            if (!string.IsNullOrWhiteSpace(idFilter)
                && row.Sheet.SheetId.ToString().IndexOf(idFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(nameFilter)
                && row.Sheet.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(pathFilter)
                && row.Path.IndexOf(pathFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            return true;
        }

        private void RefreshRows()
        {
            rows.Clear();
            List<SpriteSheetDefinition> sheets = SpriteSheetEditorAssetUtility.FindAllSpriteSheets();
            List<SpriteAnimationSetDefinition> animationSets = SpriteSheetEditorAssetUtility.FindAllAnimationSets();

            for (int i = 0; i < sheets.Count; i++)
            {
                SpriteSheetDefinition sheet = sheets[i];
                int clipCount = animationSets
                    .Where(set => set.SpriteSheet == sheet)
                    .Sum(set => set.Clips != null ? set.Clips.Count : 0);

                rows.Add(new OverviewRow
                {
                    Sheet = sheet,
                    Path = AssetDatabase.GetAssetPath(sheet),
                    ClipCount = clipCount
                });
            }

            Repaint();
        }
    }
}
