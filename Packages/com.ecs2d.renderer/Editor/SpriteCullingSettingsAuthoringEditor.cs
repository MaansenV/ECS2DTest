using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ECS2D.Rendering.Editor
{
    [CustomEditor(typeof(SpriteCullingSettingsAuthoring))]
    public sealed class SpriteCullingSettingsAuthoringEditor : UnityEditor.Editor
    {
        private const string StylesheetPath = "Packages/com.ecs2d.renderer/Editor/Styles/ECS2DInspectorStyles.uss";
        private const string HelpText = "Enable culling to skip rendering sprites outside the active camera view.";

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("ecs2d-inspector-root");

            var stylesheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (stylesheet != null)
            {
                root.styleSheets.Add(stylesheet);
            }

            var cullingEnabled = serializedObject.FindProperty("CullingEnabled");
            var cullingField = new PropertyField(cullingEnabled);
            cullingField.AddToClassList("ecs2d-prominent-toggle");
            root.Add(cullingField);

            var helpBox = new HelpBox(HelpText, HelpBoxMessageType.Info)
            {
                name = "culling-help"
            };
            helpBox.AddToClassList("ecs2d-help-box");
            root.Add(helpBox);

            return root;
        }
    }
}
