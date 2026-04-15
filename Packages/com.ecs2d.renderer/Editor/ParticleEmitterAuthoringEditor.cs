using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ECS2D.Rendering.Editor
{
    [CustomEditor(typeof(ParticleEmitterAuthoring))]
    public sealed class ParticleEmitterAuthoringEditor : UnityEditor.Editor
    {
        private const string StylesheetPath = "Packages/com.ecs2d.renderer/Editor/Styles/ECS2DInspectorStyles.uss";
        private const string EnabledHelpText = "Master enable toggle for this particle emitter";
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("ecs2d-inspector-root");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            var enabledField = CreatePropertyField("Enabled");
            enabledField.AddToClassList("ecs2d-prominent-toggle");
            root.Add(enabledField);

            var enabledHelpBox = new HelpBox(EnabledHelpText, HelpBoxMessageType.Info);
            enabledHelpBox.AddToClassList("ecs2d-help-box");
            root.Add(enabledHelpBox);

            root.Add(CreateFoldout(
                "Sprite",
                "particle-emitter-section-sprite",
                "ecs2d-section-sprite",
                "SpriteSheet",
                "SortingLayer",
                "SpriteFrameIndex"));

            var spawnFoldout = CreateFoldout(
                "Spawn",
                "particle-emitter-section-spawn",
                "ecs2d-section-spawn",
                "BurstCount",
                "SpawnRate",
                "MaxParticles",
                "DestroyEmitterAfterSeconds");
            var destroyEmitterHelpBox = new HelpBox("Set to -1 to disable auto-destroy. 0+ seconds enables auto-destroy timer.", HelpBoxMessageType.Info);
            destroyEmitterHelpBox.AddToClassList("ecs2d-help-box");
            spawnFoldout.Add(destroyEmitterHelpBox);
            root.Add(spawnFoldout);

            root.Add(CreateFoldout(
                "Lifetime & Speed",
                "particle-emitter-section-speed",
                "ecs2d-section-physics",
                "LifetimeMin",
                "LifetimeMax",
                "SpeedMin",
                "SpeedMax",
                "SpeedCurve",
                "SpeedCurveMode"));

            root.Add(CreateFoldout(
                "Scale & Color",
                "particle-emitter-section-scale-color",
                "ecs2d-section-color",
                "BaseScale",
                "BaseScaleXY",
                "ScaleCurve",
                "ScaleCurveMode",
                "StartColor",
                "EndColor"));

            root.Add(CreateFoldout(
                "Shape & Direction",
                "particle-emitter-section-shape",
                "ecs2d-section-shape",
                "CircleRadius",
                "CircleMode",
                "DirectionMode",
                "FixedDirection"));

            root.Add(CreateFoldout(
                "Rotation",
                "particle-emitter-section-rotation",
                "ecs2d-section-rotation",
                "StartRotationMinDegrees",
                "StartRotationMaxDegrees",
                "RotationSpeedMinDegrees",
                "RotationSpeedMaxDegrees"));

            return root;
        }

        private Foldout CreateFoldout(string title, string viewDataKey, string colorClass, params string[] propertyNames)
        {
            var foldout = new Foldout
            {
                text = title,
                viewDataKey = viewDataKey
            };

            foldout.AddToClassList("ecs2d-section");
            foldout.AddToClassList("ecs2d-foldout");
            foldout.AddToClassList(colorClass);

            foreach (var propertyName in propertyNames)
            {
                foldout.Add(CreatePropertyField(propertyName));
            }

            return foldout;
        }

        private PropertyField CreatePropertyField(string propertyName)
        {
            var field = new PropertyField(serializedObject.FindProperty(propertyName));
            field.AddToClassList("ecs2d-field-spacing");
            return field;
        }
    }
}
