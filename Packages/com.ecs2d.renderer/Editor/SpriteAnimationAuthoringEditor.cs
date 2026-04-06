using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ECS2D.Rendering.Editor
{
    [CustomEditor(typeof(SpriteAnimationAuthoring))]
    public sealed class SpriteAnimationAuthoringEditor : UnityEditor.Editor
    {
        private const string StylesheetPath = "Packages/com.ecs2d.renderer/Editor/Styles/ECS2DInspectorStyles.uss";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("ecs2d-inspector-root");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            var animationFoldout = new Foldout
            {
                text = "Animation",
                viewDataKey = "sprite-animation-section-animation"
            };
            animationFoldout.AddToClassList("ecs2d-section");
            animationFoldout.AddToClassList("ecs2d-foldout");
            animationFoldout.AddToClassList("ecs2d-section-animation");

            var animationSetField = new PropertyField(serializedObject.FindProperty("AnimationSet"));
            animationSetField.AddToClassList("ecs2d-field-spacing");
            animationFoldout.Add(animationSetField);

            var startAnimationField = new PropertyField(serializedObject.FindProperty("StartAnimation"));
            startAnimationField.AddToClassList("ecs2d-field-spacing");
            animationFoldout.Add(startAnimationField);

            root.Add(animationFoldout);

            var playbackFoldout = new Foldout
            {
                text = "Playback",
                viewDataKey = "sprite-animation-section-playback"
            };
            playbackFoldout.AddToClassList("ecs2d-section");
            playbackFoldout.AddToClassList("ecs2d-foldout");
            playbackFoldout.AddToClassList("ecs2d-section-animation");

            var playbackSpeedField = new PropertyField(serializedObject.FindProperty("PlaybackSpeed"));
            playbackSpeedField.AddToClassList("ecs2d-field-spacing");
            playbackFoldout.Add(playbackSpeedField);

            var playOnStartField = new PropertyField(serializedObject.FindProperty("PlayOnStart"));
            playOnStartField.AddToClassList("ecs2d-field-spacing");
            playbackFoldout.Add(playOnStartField);

            root.Add(playbackFoldout);

            return root;
        }
    }
}
