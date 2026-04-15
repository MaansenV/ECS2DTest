using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ECS2D.Rendering.Editor
{
    [CustomEditor(typeof(SpriteDataAuthoring))]
    public sealed class SpriteDataAuthoringEditor : UnityEditor.Editor
    {
        private const string StylesheetPath = "Packages/com.ecs2d.renderer/Editor/Styles/ECS2DInspectorStyles.uss";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("ecs2d-inspector-root");

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (stylesheet != null)
            {
                root.styleSheets.Add(stylesheet);
            }

            var spriteSheet = serializedObject.FindProperty("SpriteSheet");
            var sortingLayer = serializedObject.FindProperty("SortingLayer");
            var spriteFrameIndex = serializedObject.FindProperty("SpriteFrameIndex");
            var baseScale = serializedObject.FindProperty("BaseScale");
            var baseScaleXY = serializedObject.FindProperty("BaseScaleXY");
            var useAdvancedScaleXY = serializedObject.FindProperty("useAdvancedScaleXY");
            var color = serializedObject.FindProperty("Color");
            var rotationOffsetDegrees = serializedObject.FindProperty("RotationOffsetDegrees");
            var flipX = serializedObject.FindProperty("FlipX");
            var flipY = serializedObject.FindProperty("FlipY");

            // Foldout 1: Sprite
            var spriteFoldout = new Foldout { text = "Sprite", viewDataKey = "sprite-data-section-sprite" };
            spriteFoldout.AddToClassList("ecs2d-section");
            spriteFoldout.AddToClassList("ecs2d-foldout");
            spriteFoldout.AddToClassList("ecs2d-section-sprite");
            spriteFoldout.Add(new PropertyField(spriteSheet));
            spriteFoldout.Add(new PropertyField(sortingLayer));
            spriteFoldout.Add(new PropertyField(spriteFrameIndex));
            root.Add(spriteFoldout);

            // Foldout 2: Transform
            var transformFoldout = new Foldout { text = "Transform", viewDataKey = "sprite-data-section-transform" };
            transformFoldout.AddToClassList("ecs2d-section");
            transformFoldout.AddToClassList("ecs2d-foldout");
            transformFoldout.AddToClassList("ecs2d-section-shape");

            bool advancedMode = useAdvancedScaleXY.boolValue;

            var scaleModeToggle = new Toggle("Advanced XY Scale")
            {
                value = advancedMode
            };
            scaleModeToggle.AddToClassList("ecs2d-field-spacing");

            var baseScaleField = new PropertyField(baseScale);
            var baseScaleXYField = new PropertyField(baseScaleXY);

            void RefreshScaleMode(bool isAdvanced)
            {
                baseScaleField.style.display = isAdvanced ? DisplayStyle.None : DisplayStyle.Flex;
                baseScaleXYField.style.display = isAdvanced ? DisplayStyle.Flex : DisplayStyle.None;
                if (useAdvancedScaleXY.boolValue != isAdvanced)
                {
                    useAdvancedScaleXY.boolValue = isAdvanced;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            scaleModeToggle.RegisterValueChangedCallback(evt => RefreshScaleMode(evt.newValue));

            transformFoldout.Add(scaleModeToggle);
            transformFoldout.Add(baseScaleField);
            transformFoldout.Add(baseScaleXYField);
            transformFoldout.Add(new PropertyField(rotationOffsetDegrees));
            RefreshScaleMode(advancedMode);
            root.Add(transformFoldout);

            var displayFoldout = new Foldout { text = "Display", viewDataKey = "sprite-data-section-display" };
            displayFoldout.AddToClassList("ecs2d-section");
            displayFoldout.AddToClassList("ecs2d-foldout");
            displayFoldout.AddToClassList("ecs2d-section-color");
            displayFoldout.Add(new PropertyField(color));
            displayFoldout.Add(new PropertyField(flipX));
            displayFoldout.Add(new PropertyField(flipY));
            root.Add(displayFoldout);

            return root;
        }
    }
}
