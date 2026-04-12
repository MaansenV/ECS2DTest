using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ECS2D.Rendering.Editor
{
    public sealed class SpriteSheetWizardWindow : EditorWindow
    {
        private enum WizardStep
        {
            Setup = 0,
            Sheet = 1,
            Animation = 2
        }

        private enum SaveMode
        {
            CreateNew = 0,
            UpdateExisting = 1
        }

        private enum AnimationUpdateMode
        {
            ReplaceAll = 0,
            MergeByName = 1,
            KeepExisting = 2
        }

        private const string WindowTitle = "SpriteSheet Wizard";
        private const float PreviewSize = 96f;

        private Vector2 scrollPosition;
        private WizardStep currentStep;
        private UnityEngine.Object sourceObject;
        private SpriteSheetEditorResolvedSource resolvedSource;
        private string outputFolder = SpriteSheetEditorAssetUtility.DefaultOutputFolder;
        private int sheetId = 1;
        private string sheetAssetName = "SpriteSheetDefinition";
        private string animationSetAssetName = "SpriteAnimationSetDefinition";
        private SaveMode saveMode;
        private SpriteSheetDefinition existingSheet;
        private SpriteAnimationSetDefinition existingAnimationSet;
        private bool applySheetChanges = true;
        private bool applyAnimationSetChanges = true;
        private AnimationUpdateMode animationUpdateMode = AnimationUpdateMode.ReplaceAll;

        private Material baseMaterial;
        private Texture2D texture;
        private Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        private int initialCapacity = 256;
        private int capacityStep = 256;
        private int columns = 1;
        private int rows = 1;
        private bool showAdvancedSheetSettings;

        private List<SpriteAnimationClipDraft> clipDrafts = new List<SpriteAnimationClipDraft>();
        private string clipSuggestionMessage = string.Empty;
        private string saveStatusMessage = string.Empty;
        private MessageType saveStatusType = MessageType.None;

        [MenuItem("Tools/ECS2D/Sprite Sheet Wizard")]
        public static void OpenWindow()
        {
            OpenForObject(Selection.activeObject);
        }

        [MenuItem("Assets/ECS2D/Open Sprite Sheet Wizard", true)]
        private static bool ValidateOpenFromSelection()
        {
            return IsSupportedSelection(Selection.activeObject);
        }

        [MenuItem("Assets/ECS2D/Open Sprite Sheet Wizard")]
        private static void OpenFromSelection()
        {
            OpenForObject(Selection.activeObject);
        }

        public static void OpenForObject(UnityEngine.Object source)
        {
            var window = GetWindow<SpriteSheetWizardWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(640f, 520f);
            window.Initialize(source);
            window.Show();
        }

        private static bool IsSupportedSelection(UnityEngine.Object value)
        {
            return value == null
                || value is Texture2D
                || value is Sprite
                || value is SpriteSheetDefinition
                || value is SpriteAnimationSetDefinition;
        }

        private void Initialize(UnityEngine.Object initialSource)
        {
            sheetId = SpriteSheetEditorAssetUtility.SuggestNextSheetId();
            clipDrafts = new List<SpriteAnimationClipDraft>
            {
                new SpriteAnimationClipDraft()
            };
            outputFolder = SpriteSheetEditorAssetUtility.SuggestOutputFolder(initialSource);
            sourceObject = initialSource;
            currentStep = WizardStep.Setup;
            saveMode = SaveMode.CreateNew;
            existingSheet = null;
            existingAnimationSet = null;
            saveStatusMessage = string.Empty;
            saveStatusType = MessageType.None;
            ResolveSourceAndPrefill();
        }

        private void OnEnable()
        {
            if (sheetId <= 0)
            {
                Initialize(sourceObject != null ? sourceObject : Selection.activeObject);
            }
        }

        private void OnGUI()
        {
            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            switch (currentStep)
            {
                case WizardStep.Setup:
                    DrawSetupStep();
                    break;
                case WizardStep.Sheet:
                    DrawSheetStep();
                    break;
                case WizardStep.Animation:
                    DrawAnimationStep();
                    break;
            }

            EditorGUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SpriteSheet Wizard", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Create or update one SpriteSheetDefinition and one SpriteAnimationSetDefinition in 3 steps.", EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawStepPill("1. Setup", currentStep == WizardStep.Setup);
                DrawStepPill("2. Sheet", currentStep == WizardStep.Sheet);
                DrawStepPill("3. Animation", currentStep == WizardStep.Animation);
            }

            EditorGUILayout.Space();
        }

        private void DrawStepPill(string text, bool selected)
        {
            var style = new GUIStyle(EditorStyles.miniButtonMid)
            {
                fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
                fixedHeight = 24f
            };

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = selected ? new Color(0.35f, 0.7f, 1f, 1f) : previousColor;
            GUILayout.Button(text, style, GUILayout.ExpandWidth(true));
            GUI.backgroundColor = previousColor;
        }

        private void DrawSetupStep()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            sourceObject = EditorGUILayout.ObjectField(new GUIContent("Source Asset", "Optional source Texture2D, Sprite, SpriteSheetDefinition, or SpriteAnimationSetDefinition."), sourceObject, typeof(UnityEngine.Object), false);
            if (EditorGUI.EndChangeCheck())
            {
                ResolveSourceAndPrefill();
            }

            if (GUILayout.Button("Use Current Selection", GUILayout.Width(180f)))
            {
                sourceObject = Selection.activeObject;
                ResolveSourceAndPrefill();
            }

            EditorGUILayout.HelpBox(resolvedSource?.DetectionMessage ?? "Select a source asset or proceed with manual entry.", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);
            DrawOutputFolderField();

            EditorGUI.BeginChangeCheck();
            sheetId = EditorGUILayout.IntField(new GUIContent("Sheet ID", "A unique ID loaded by SpriteSheetDatabase at runtime."), sheetId);
            if (EditorGUI.EndChangeCheck())
            {
                sheetId = Mathf.Max(0, sheetId);
                RefreshTargetsFromSheetId();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                sheetAssetName = EditorGUILayout.TextField(new GUIContent("Sheet Asset Name"), sheetAssetName);
                if (GUILayout.Button("Suggest", GUILayout.Width(80f)))
                {
                    SuggestNamesFromCurrentState();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                animationSetAssetName = EditorGUILayout.TextField(new GUIContent("Animation Set Name"), animationSetAssetName);
                if (GUILayout.Button("Suggest", GUILayout.Width(80f)))
                {
                    SuggestNamesFromCurrentState();
                }
            }

            saveMode = (SaveMode)EditorGUILayout.EnumPopup(new GUIContent("Target Mode"), saveMode);
            if (saveMode == SaveMode.UpdateExisting)
            {
                EditorGUI.BeginChangeCheck();
                existingSheet = (SpriteSheetDefinition)EditorGUILayout.ObjectField(new GUIContent("Existing Sheet"), existingSheet, typeof(SpriteSheetDefinition), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (existingSheet != null)
                    {
                        sheetId = existingSheet.SheetId;
                        RefreshExistingAnimationSetSelection();
                        LoadExistingSheetIntoDraft(existingSheet);
                    }
                }

                EditorGUI.BeginChangeCheck();
                existingAnimationSet = (SpriteAnimationSetDefinition)EditorGUILayout.ObjectField(new GUIContent("Existing Animation Set"), existingAnimationSet, typeof(SpriteAnimationSetDefinition), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if (existingAnimationSet != null && existingAnimationSet.SpriteSheet != null)
                    {
                        existingSheet = existingAnimationSet.SpriteSheet;
                        sheetId = existingSheet.SheetId;
                        resolvedSource = SpriteSheetEditorAssetUtility.ResolveSource(existingSheet);
                        LoadExistingSheetIntoDraft(existingSheet);
                    }
                    else if (existingSheet != null)
                    {
                        LoadExistingSheetIntoDraft(existingSheet);
                    }
                }
            }

            DrawNamingConventionHints();
            DrawSetupValidationPreview();
        }

        private void DrawSheetStep()
        {
            EditorGUILayout.LabelField("Sheet Configuration", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            texture = (Texture2D)EditorGUILayout.ObjectField(new GUIContent("Texture"), texture, typeof(Texture2D), false);
            baseMaterial = (Material)EditorGUILayout.ObjectField(new GUIContent("Base Material"), baseMaterial, typeof(Material), false);
            columns = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Columns"), columns));
            rows = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Rows"), rows));
            if (EditorGUI.EndChangeCheck())
            {
                RegenerateClipSuggestionsIfHelpful();
            }

            showAdvancedSheetSettings = EditorGUILayout.Foldout(showAdvancedSheetSettings, "Advanced SpriteSheet settings", true);
            if (showAdvancedSheetSettings)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    worldBounds = EditorGUILayout.BoundsField(new GUIContent("World Bounds"), worldBounds);
                    initialCapacity = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Initial Capacity"), initialCapacity));
                    capacityStep = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Capacity Step"), capacityStep));
                }
            }

            DrawPreview();
            DrawSheetValidationPreview();
        }

        private void DrawAnimationStep()
        {
            EditorGUILayout.LabelField("Animation Configuration", EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(clipSuggestionMessage))
            {
                EditorGUILayout.HelpBox(clipSuggestionMessage, MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Regenerate Clip Suggestions", GUILayout.Width(200f)))
                {
                    GenerateClipSuggestions();
                }

                if (GUILayout.Button("Add Clip", GUILayout.Width(100f)))
                {
                    clipDrafts.Add(new SpriteAnimationClipDraft());
                }
            }

            EditorGUILayout.Space(4f);
            for (int i = 0; i < clipDrafts.Count; i++)
            {
                DrawClipEditor(i);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Apply Options", EditorStyles.boldLabel);
            if (saveMode == SaveMode.UpdateExisting)
            {
                applySheetChanges = EditorGUILayout.ToggleLeft("Apply SpriteSheet changes", applySheetChanges);
                applyAnimationSetChanges = EditorGUILayout.ToggleLeft("Apply AnimationSet changes", applyAnimationSetChanges);
                if (existingAnimationSet == null)
                {
                    EditorGUILayout.HelpBox("No existing AnimationSet is linked to this sheet yet. If you keep animation changes enabled, the wizard will create one.", MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!applyAnimationSetChanges))
                {
                    animationUpdateMode = (AnimationUpdateMode)EditorGUILayout.EnumPopup(new GUIContent("Animation Update Mode"), animationUpdateMode);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("New assets will be created for both the SpriteSheet and the AnimationSet.", MessageType.None);
            }

            var validationMessages = CollectValidationMessages();
            MessageType validationType = validationMessages.Count == 0 ? MessageType.Info : MessageType.Error;
            string validationText = validationMessages.Count == 0
                ? BuildReadyToSaveSummary()
                : string.Join("\n", validationMessages.Select(message => $"• {message}"));
            EditorGUILayout.HelpBox(validationText, validationType);

            if (!string.IsNullOrWhiteSpace(saveStatusMessage))
            {
                EditorGUILayout.HelpBox(saveStatusMessage, saveStatusType);
            }
        }

        private void DrawClipEditor(int index)
        {
            var clip = clipDrafts[index];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Clip {index + 1}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        clipDrafts.RemoveAt(index);
                        GUIUtility.ExitGUI();
                    }
                }

                clip.Name = EditorGUILayout.TextField("Name", clip.Name);
                using (new EditorGUILayout.HorizontalScope())
                {
                    clip.Row = Mathf.Max(0, EditorGUILayout.IntField("Row", clip.Row));
                    clip.StartColumn = Mathf.Max(0, EditorGUILayout.IntField("Start Column", clip.StartColumn));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    clip.FrameCount = Mathf.Max(1, EditorGUILayout.IntField("Frame Count", clip.FrameCount));
                    clip.FrameRate = Mathf.Max(0f, EditorGUILayout.FloatField("Frame Rate", clip.FrameRate));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    clip.Loop = EditorGUILayout.ToggleLeft("Loop", clip.Loop, GUILayout.Width(120f));
                    clip.PingPong = EditorGUILayout.ToggleLeft("Ping Pong", clip.PingPong, GUILayout.Width(120f));
                }
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(currentStep == WizardStep.Setup))
                {
                    if (GUILayout.Button("Back", GUILayout.Width(100f)))
                    {
                        currentStep -= 1;
                        saveStatusMessage = string.Empty;
                    }
                }

                GUILayout.FlexibleSpace();

                if (currentStep != WizardStep.Animation)
                {
                    if (GUILayout.Button("Next", GUILayout.Width(100f)))
                    {
                        currentStep += 1;
                        saveStatusMessage = string.Empty;
                    }
                }
                else
                {
                    using (new EditorGUI.DisabledScope(CollectValidationMessages().Count > 0))
                    {
                        if (GUILayout.Button("Save Assets", GUILayout.Width(120f)))
                        {
                            SaveAssets();
                        }
                    }
                }
            }
        }

        private void DrawOutputFolderField()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder"), outputFolder);
                if (GUILayout.Button("Browse", GUILayout.Width(80f)))
                {
                    string absoluteFolder = EditorUtility.OpenFolderPanel("Choose SpriteSheet output folder", Application.dataPath, string.Empty);
                    if (!string.IsNullOrWhiteSpace(absoluteFolder)
                        && SpriteSheetEditorAssetUtility.TryProjectRelativePath(absoluteFolder, out string relativeFolder))
                    {
                        outputFolder = relativeFolder.Replace('\\', '/');
                    }
                }

                if (GUILayout.Button("Use Recommended", GUILayout.Width(120f)))
                {
                    outputFolder = SpriteSheetEditorAssetUtility.SuggestOutputFolder(sourceObject);
                }
            }
        }

        private void DrawNamingConventionHints()
        {
            if (!SpriteSheetEditorAssetUtility.IsRecommendedAssetName(sheetAssetName, "Sheet"))
            {
                EditorGUILayout.HelpBox("Recommended naming: end SpriteSheet assets with 'Sheet'.", MessageType.Warning);
            }

            if (!SpriteSheetEditorAssetUtility.IsRecommendedAssetName(animationSetAssetName, "Animations"))
            {
                EditorGUILayout.HelpBox("Recommended naming: end animation set assets with 'Animations'.", MessageType.Warning);
            }
        }

        private void DrawSetupValidationPreview()
        {
            if (saveMode == SaveMode.UpdateExisting && existingSheet != null)
            {
                EditorGUILayout.HelpBox($"Existing SpriteSheet '{existingSheet.name}' will be updated for ID {existingSheet.SheetId}.", MessageType.Info);
            }
            else if (SpriteSheetEditorAssetUtility.TryGetSheetById(sheetId, out var duplicateSheet))
            {
                EditorGUILayout.HelpBox($"Sheet ID {sheetId} already exists on '{duplicateSheet.name}'. Switch to Update Existing or choose a new ID.", MessageType.Warning);
            }
        }

        private void DrawSheetValidationPreview()
        {
            if (resolvedSource != null && !resolvedSource.HasDetectedGrid && resolvedSource.Sprites.Count > 1)
            {
                EditorGUILayout.HelpBox("The selected source did not produce a clean grid automatically. Enter Columns and Rows manually and review the generated clips before saving.", MessageType.Warning);
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            Rect previewRect = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.Width(PreviewSize), GUILayout.Height(PreviewSize));
            GUI.Box(previewRect, GUIContent.none);

            Sprite previewSprite = resolvedSource?.PreviewSprite;
            if (previewSprite != null)
            {
                Rect uvRect = new Rect(
                    previewSprite.textureRect.x / previewSprite.texture.width,
                    previewSprite.textureRect.y / previewSprite.texture.height,
                    previewSprite.textureRect.width / previewSprite.texture.width,
                    previewSprite.textureRect.height / previewSprite.texture.height);
                GUI.DrawTextureWithTexCoords(previewRect, previewSprite.texture, uvRect, true);
                return;
            }

            if (texture != null)
            {
                float safeColumns = Mathf.Max(1, columns);
                float safeRows = Mathf.Max(1, rows);
                Rect frameZeroUv = new Rect(
                    0f,
                    1f - (1f / safeRows),
                    1f / safeColumns,
                    1f / safeRows);
                GUI.DrawTextureWithTexCoords(previewRect, texture, frameZeroUv, true);
                Rect labelRect = new Rect(previewRect.x, previewRect.yMax + 4f, 120f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, "Frame 0 Preview", EditorStyles.miniLabel);
                return;
            }

            EditorGUI.DropShadowLabel(previewRect, "No preview");
        }

        private void ResolveSourceAndPrefill()
        {
            resolvedSource = SpriteSheetEditorAssetUtility.ResolveSource(sourceObject);
            outputFolder = string.IsNullOrWhiteSpace(outputFolder)
                ? SpriteSheetEditorAssetUtility.SuggestOutputFolder(sourceObject)
                : outputFolder;

            if (sourceObject is SpriteSheetDefinition selectedSheet)
            {
                saveMode = SaveMode.UpdateExisting;
                existingSheet = selectedSheet;
                RefreshExistingAnimationSetSelection();
                LoadExistingSheetIntoDraft(selectedSheet);
            }
            else if (sourceObject is SpriteAnimationSetDefinition selectedAnimationSet)
            {
                saveMode = SaveMode.UpdateExisting;
                existingAnimationSet = selectedAnimationSet;
                existingSheet = selectedAnimationSet.SpriteSheet;
                if (existingSheet != null)
                {
                    resolvedSource = SpriteSheetEditorAssetUtility.ResolveSource(existingSheet);
                    LoadExistingSheetIntoDraft(existingSheet);
                    sheetId = existingSheet.SheetId;
                }
            }
            else
            {
                if (resolvedSource != null)
                {
                    texture = resolvedSource.Texture != null ? resolvedSource.Texture : texture;
                    if (resolvedSource.HasDetectedGrid)
                    {
                        columns = Mathf.Max(1, resolvedSource.DetectedColumns);
                        rows = Mathf.Max(1, resolvedSource.DetectedRows);
                    }

                    string sourceBaseName = resolvedSource.BaseName;
                    sheetAssetName = SpriteSheetEditorAssetUtility.SuggestAssetName(sourceBaseName, "Sheet");
                    animationSetAssetName = SpriteSheetEditorAssetUtility.SuggestAssetName(sourceBaseName, "Animations");
                }

                RefreshTargetsFromSheetId();
            }

            GenerateClipSuggestions();
            Repaint();
        }

        private void RefreshTargetsFromSheetId()
        {
            if (SpriteSheetEditorAssetUtility.TryGetSheetById(sheetId, out var duplicateSheet))
            {
                if (saveMode == SaveMode.UpdateExisting || existingSheet == null)
                {
                    existingSheet = duplicateSheet;
                    saveMode = SaveMode.UpdateExisting;
                    RefreshExistingAnimationSetSelection();
                    LoadExistingSheetIntoDraft(existingSheet);
                }
            }
            else if (saveMode == SaveMode.CreateNew)
            {
                existingSheet = null;
                existingAnimationSet = null;
            }
        }

        private void RefreshExistingAnimationSetSelection()
        {
            if (existingSheet == null)
            {
                existingAnimationSet = null;
                return;
            }

            var linkedAnimationSets = SpriteSheetEditorAssetUtility.FindAnimationSetsForSheet(existingSheet);
            if (existingAnimationSet == null || existingAnimationSet.SpriteSheet != existingSheet)
            {
                existingAnimationSet = linkedAnimationSets.FirstOrDefault();
            }
        }

        private void LoadExistingSheetIntoDraft(SpriteSheetDefinition sheet)
        {
            if (sheet == null)
            {
                return;
            }

            sheetId = sheet.SheetId;
            texture = sheet.Texture;
            baseMaterial = sheet.BaseMaterial;
            columns = Mathf.Max(1, sheet.Columns);
            rows = Mathf.Max(1, sheet.Rows);
            worldBounds = sheet.WorldBounds;
            initialCapacity = sheet.InitialCapacity;
            capacityStep = sheet.CapacityStep;
            sheetAssetName = sheet.name;

            if (existingAnimationSet != null)
            {
                animationSetAssetName = existingAnimationSet.name;
                clipDrafts = existingAnimationSet.Clips.Select(SpriteAnimationClipDraft.FromClip).ToList();
                return;
            }

            clipDrafts = SpriteSheetEditorAssetUtility.BuildClipSuggestions(resolvedSource, columns, rows, out clipSuggestionMessage);
        }

        private void SuggestNamesFromCurrentState()
        {
            string baseName = resolvedSource != null && !string.IsNullOrWhiteSpace(resolvedSource.BaseName)
                ? resolvedSource.BaseName
                : $"Sheet{sheetId}";
            sheetAssetName = SpriteSheetEditorAssetUtility.SuggestAssetName(baseName, "Sheet");
            animationSetAssetName = SpriteSheetEditorAssetUtility.SuggestAssetName(baseName, "Animations");
        }

        private void GenerateClipSuggestions()
        {
            clipDrafts = SpriteSheetEditorAssetUtility.BuildClipSuggestions(resolvedSource, columns, rows, out clipSuggestionMessage);

            if (saveMode == SaveMode.UpdateExisting && existingAnimationSet != null && existingAnimationSet.Clips != null && existingAnimationSet.Clips.Count > 0)
            {
                clipDrafts = existingAnimationSet.Clips.Select(SpriteAnimationClipDraft.FromClip).ToList();
                clipSuggestionMessage = $"Loaded {clipDrafts.Count} existing clip(s) from '{existingAnimationSet.name}'.";
            }
        }

        private void RegenerateClipSuggestionsIfHelpful()
        {
            if (clipDrafts == null || clipDrafts.Count == 0 || clipDrafts.Count == 1 && clipDrafts[0].Name == "default")
            {
                GenerateClipSuggestions();
            }
        }

        private List<string> CollectValidationMessages()
        {
            var messages = new List<string>();

            if (!SpriteSheetEditorAssetUtility.IsValidRuntimeOutputFolder(outputFolder, out string folderMessage))
            {
                messages.Add(folderMessage);
            }

            if (string.IsNullOrWhiteSpace(sheetAssetName))
            {
                messages.Add("Sheet asset name is required.");
            }

            if (string.IsNullOrWhiteSpace(animationSetAssetName))
            {
                messages.Add("Animation set asset name is required.");
            }

            if (texture == null)
            {
                messages.Add("Texture is required.");
            }

            if (columns <= 0 || rows <= 0)
            {
                messages.Add("Columns and Rows must both be greater than zero.");
            }

            if (saveMode == SaveMode.CreateNew && SpriteSheetEditorAssetUtility.TryGetSheetById(sheetId, out var duplicateSheet))
            {
                messages.Add($"Sheet ID {sheetId} already exists on '{duplicateSheet.name}'. Choose Update Existing or a new ID.");
            }

            if (saveMode == SaveMode.UpdateExisting && existingSheet == null)
            {
                messages.Add("Select an existing SpriteSheet to update.");
            }

            if (saveMode == SaveMode.UpdateExisting && existingAnimationSet == null && !applyAnimationSetChanges)
            {
                messages.Add("No existing AnimationSet is selected. Keep animation changes enabled so the wizard can create one.");
            }

            if (clipDrafts == null || clipDrafts.Count == 0)
            {
                messages.Add("At least one animation clip is required.");
            }
            else
            {
                ValidateClipDrafts(messages);
            }

            return messages;
        }

        private void ValidateClipDrafts(List<string> messages)
        {
            int safeColumns = Mathf.Max(1, columns);
            int safeRows = Mathf.Max(1, rows);
            int totalFrameCount = safeColumns * safeRows;
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < clipDrafts.Count; i++)
            {
                SpriteAnimationClipDraft clip = clipDrafts[i];
                string clipName = clip.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(clipName))
                {
                    messages.Add($"Clip {i + 1} is missing a name.");
                    continue;
                }

                if (!seenNames.Add(clipName))
                {
                    messages.Add($"Clip '{clipName}' is duplicated.");
                }

                if (clip.FrameCount <= 0)
                {
                    messages.Add($"Clip '{clipName}' must have at least one frame.");
                }

                if (clip.Row < 0 || clip.Row >= safeRows)
                {
                    messages.Add($"Clip '{clipName}' row {clip.Row} is outside the configured grid ({safeRows} rows).");
                }

                if (clip.StartColumn < 0 || clip.StartColumn >= safeColumns)
                {
                    messages.Add($"Clip '{clipName}' start column {clip.StartColumn} is outside the configured grid ({safeColumns} columns).");
                }

                if (clip.StartColumn + clip.FrameCount > safeColumns)
                {
                    messages.Add($"Clip '{clipName}' extends past the configured columns.");
                }

                int startIndex = (clip.Row * safeColumns) + clip.StartColumn;
                if (startIndex < 0 || startIndex + clip.FrameCount > totalFrameCount)
                {
                    messages.Add($"Clip '{clipName}' resolves outside the sprite sheet frame range.");
                }
            }
        }

        private string BuildReadyToSaveSummary()
        {
            string sheetSummary = saveMode == SaveMode.UpdateExisting && existingSheet != null
                ? $"Update SpriteSheet '{existingSheet.name}'"
                : $"Create SpriteSheet '{sheetAssetName}'";
            string animationSummary = saveMode == SaveMode.UpdateExisting && existingAnimationSet != null
                ? $"Update AnimationSet '{existingAnimationSet.name}'"
                : $"Create AnimationSet '{animationSetAssetName}'";
            return $"Ready to save.\n• {sheetSummary}\n• {animationSummary}\n• Output: {outputFolder}\n• Grid: {columns}x{rows}\n• Clips: {clipDrafts.Count}";
        }

        private void SaveAssets()
        {
            bool startedAssetEditing = false;
            try
            {
                SpriteSheetEditorAssetUtility.EnsureFolderExists(outputFolder);
                AssetDatabase.StartAssetEditing();
                startedAssetEditing = true;

                SpriteSheetDefinition sheetTarget = saveMode == SaveMode.UpdateExisting && existingSheet != null
                    ? existingSheet
                    : CreateOrLoadAsset<SpriteSheetDefinition>(outputFolder, sheetAssetName);
                ApplySheetToAsset(sheetTarget);

                SpriteAnimationSetDefinition animationTarget = existingAnimationSet;
                bool shouldCreateOrUpdateAnimationSet = saveMode == SaveMode.CreateNew || applyAnimationSetChanges;
                if (shouldCreateOrUpdateAnimationSet)
                {
                    animationTarget = saveMode == SaveMode.UpdateExisting && existingAnimationSet != null
                        ? existingAnimationSet
                        : CreateOrLoadAsset<SpriteAnimationSetDefinition>(outputFolder, animationSetAssetName);
                    ApplyAnimationSetToAsset(animationTarget, sheetTarget);
                }

                EditorUtility.SetDirty(sheetTarget);
                if (animationTarget != null && shouldCreateOrUpdateAnimationSet)
                {
                    EditorUtility.SetDirty(animationTarget);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = sheetTarget;
                EditorGUIUtility.PingObject(sheetTarget);
                saveStatusMessage = animationTarget != null && shouldCreateOrUpdateAnimationSet
                    ? $"Saved SpriteSheet '{sheetTarget.name}' and AnimationSet '{animationTarget.name}'."
                    : $"Saved SpriteSheet '{sheetTarget.name}'. AnimationSet was left unchanged.";
                saveStatusType = MessageType.Info;
                existingSheet = sheetTarget;
                if (animationTarget != null)
                {
                    existingAnimationSet = animationTarget;
                }

                saveMode = SaveMode.UpdateExisting;
                RefreshExistingAnimationSetSelection();
            }
            catch (Exception exception)
            {
                saveStatusMessage = exception.Message;
                saveStatusType = MessageType.Error;
                Debug.LogException(exception);
            }
            finally
            {
                if (startedAssetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        private T CreateOrLoadAsset<T>(string folder, string assetName) where T : ScriptableObject
        {
            string sanitizedName = SpriteSheetEditorAssetUtility.SanitizeName(assetName);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder.TrimEnd('/')}/{sanitizedName}.asset");
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(assetPath);
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private void ApplySheetToAsset(SpriteSheetDefinition target)
        {
            if (target == null)
            {
                throw new InvalidOperationException("SpriteSheet target asset could not be created.");
            }

            if (saveMode == SaveMode.UpdateExisting && !applySheetChanges)
            {
                return;
            }

            Undo.RecordObject(target, "Apply SpriteSheet Wizard");
            var serializedObject = new SerializedObject(target);
            serializedObject.FindProperty("sheetId").intValue = sheetId;
            serializedObject.FindProperty("baseMaterial").objectReferenceValue = baseMaterial;
            serializedObject.FindProperty("texture").objectReferenceValue = texture;
            serializedObject.FindProperty("worldBounds").boundsValue = worldBounds;
            serializedObject.FindProperty("initialCapacity").intValue = Mathf.Max(1, initialCapacity);
            serializedObject.FindProperty("capacityStep").intValue = Mathf.Max(1, capacityStep);
            serializedObject.FindProperty("autoGenerateGridFrames").boolValue = true;
            serializedObject.FindProperty("columns").intValue = Mathf.Max(1, columns);
            serializedObject.FindProperty("rows").intValue = Mathf.Max(1, rows);
            serializedObject.FindProperty("frames").arraySize = 0;
            serializedObject.ApplyModifiedProperties();
            target.name = SpriteSheetEditorAssetUtility.SanitizeName(sheetAssetName);
        }

        private void ApplyAnimationSetToAsset(SpriteAnimationSetDefinition target, SpriteSheetDefinition sheetTarget)
        {
            if (target == null)
            {
                throw new InvalidOperationException("AnimationSet target asset could not be created.");
            }

            if (saveMode == SaveMode.UpdateExisting && !applyAnimationSetChanges)
            {
                return;
            }

            Undo.RecordObject(target, "Apply SpriteSheet Wizard AnimationSet");
            target.name = SpriteSheetEditorAssetUtility.SanitizeName(animationSetAssetName);
            target.SpriteSheet = sheetTarget;

            var newClips = clipDrafts.Select(draft => draft.ToClip()).ToList();
            switch (animationUpdateMode)
            {
                case AnimationUpdateMode.MergeByName:
                    target.Clips = MergeClipsByName(target.Clips, newClips);
                    break;
                case AnimationUpdateMode.KeepExisting:
                    if (target.Clips == null || target.Clips.Count == 0)
                    {
                        target.Clips = newClips;
                    }
                    break;
                default:
                    target.Clips = newClips;
                    break;
            }
        }

        private List<SpriteAnimationClip> MergeClipsByName(List<SpriteAnimationClip> existingClips, List<SpriteAnimationClip> incomingClips)
        {
            existingClips ??= new List<SpriteAnimationClip>();
            var merged = existingClips.ToDictionary(clip => clip.Name ?? string.Empty, clip => clip, StringComparer.Ordinal);
            for (int i = 0; i < incomingClips.Count; i++)
            {
                merged[incomingClips[i].Name ?? string.Empty] = incomingClips[i];
            }

            return merged.Values.OrderBy(clip => clip.Name, StringComparer.Ordinal).ToList();
        }
    }
}
