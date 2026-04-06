using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ECS2D.Rendering.Editor
{
    [CustomEditor(typeof(EntitiesReferenceAuthoring))]
    public sealed class EntitiesReferenceAuthoringEditor : UnityEditor.Editor
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

            var section = new VisualElement();
            section.AddToClassList("ecs2d-section");

            var field = new PropertyField(serializedObject.FindProperty("bulletPrefab"));
            field.AddToClassList("ecs2d-field-spacing");
            section.Add(field);

            var helpBox = new HelpBox("Assign the GameObject that will be baked into the BulletPrefab entity reference.", HelpBoxMessageType.Info);
            helpBox.AddToClassList("ecs2d-help-box");
            section.Add(helpBox);

            root.Add(section);
            return root;
        }
    }
}
