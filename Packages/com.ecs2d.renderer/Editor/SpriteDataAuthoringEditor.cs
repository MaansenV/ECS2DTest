using UnityEditor;
using UnityEditor.UIElements;
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
            transformFoldout.Add(new PropertyField(baseScale));
            transformFoldout.Add(new PropertyField(rotationOffsetDegrees));
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
