using UnityEditor;
using UnityEngine;

namespace ECS2D.Rendering.Editor
{
    [CustomPropertyDrawer(typeof(SpriteAnimationClip))]
    public sealed class SpriteAnimationClipDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            const int lineCount = 6;
            return (EditorGUIUtility.singleLineHeight * lineCount) + (VerticalSpacing * (lineCount - 1));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                SerializedProperty nameProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.Name));
                SerializedProperty rowProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.Row));
                SerializedProperty startColumnProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.StartColumn));
                SerializedProperty frameCountProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.FrameCount));
                SerializedProperty frameRateProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.FrameRate));
                SerializedProperty loopProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.Loop));
                SerializedProperty pingPongProperty = property.FindPropertyRelative(nameof(SpriteAnimationClip.PingPong));

                Rect currentRect = NextLine(position, 1);
                EditorGUI.PropertyField(currentRect, nameProperty);

                currentRect = NextLine(position, 2);
                float halfWidth = (currentRect.width - 6f) * 0.5f;
                Rect leftRect = new Rect(currentRect.x, currentRect.y, halfWidth, currentRect.height);
                Rect rightRect = new Rect(currentRect.x + halfWidth + 6f, currentRect.y, halfWidth, currentRect.height);
                EditorGUI.PropertyField(leftRect, rowProperty);
                EditorGUI.PropertyField(rightRect, startColumnProperty);

                currentRect = NextLine(position, 3);
                leftRect = new Rect(currentRect.x, currentRect.y, halfWidth, currentRect.height);
                rightRect = new Rect(currentRect.x + halfWidth + 6f, currentRect.y, halfWidth, currentRect.height);
                EditorGUI.PropertyField(leftRect, frameCountProperty);
                EditorGUI.PropertyField(rightRect, frameRateProperty);

                currentRect = NextLine(position, 4);
                leftRect = new Rect(currentRect.x, currentRect.y, halfWidth, currentRect.height);
                rightRect = new Rect(currentRect.x + halfWidth + 6f, currentRect.y, halfWidth, currentRect.height);
                using (new EditorGUI.DisabledScope(true))
                {
                    float approximateDurationSeconds = ComputeApproximateDurationSeconds(frameCountProperty.intValue, frameRateProperty.floatValue, pingPongProperty.boolValue);
                    EditorGUI.TextField(leftRect, new GUIContent("Dauer ca. (s)"), approximateDurationSeconds.ToString("0.00"));
                }

                EditorGUI.LabelField(rightRect, BuildDurationHint(frameCountProperty.intValue, frameRateProperty.floatValue, pingPongProperty.boolValue), EditorStyles.miniLabel);

                currentRect = NextLine(position, 5);
                leftRect = new Rect(currentRect.x, currentRect.y, halfWidth, currentRect.height);
                rightRect = new Rect(currentRect.x + halfWidth + 6f, currentRect.y, halfWidth, currentRect.height);
                EditorGUI.PropertyField(leftRect, loopProperty);
                EditorGUI.PropertyField(rightRect, pingPongProperty);

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        private static float ComputeApproximateDurationSeconds(int frameCount, float frameRate, bool pingPong)
        {
            if (frameCount <= 0 || frameRate <= 0f)
            {
                return 0f;
            }

            int effectiveFrameSteps = pingPong && frameCount > 1
                ? (frameCount * 2) - 2
                : frameCount;
            return effectiveFrameSteps / frameRate;
        }

        private static string BuildDurationHint(int frameCount, float frameRate, bool pingPong)
        {
            if (frameCount <= 0)
            {
                return "No frames";
            }

            if (frameRate <= 0f)
            {
                return "FrameRate must be > 0";
            }

            return pingPong && frameCount > 1
                ? "One full ping-pong cycle"
                : "One forward pass";
        }

        private static Rect NextLine(Rect totalRect, int lineIndex)
        {
            float y = totalRect.y + ((EditorGUIUtility.singleLineHeight + VerticalSpacing) * lineIndex);
            return new Rect(totalRect.x, y, totalRect.width, EditorGUIUtility.singleLineHeight);
        }
    }
}
