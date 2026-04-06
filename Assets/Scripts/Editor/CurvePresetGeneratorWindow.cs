using System;
using UnityEditor;
using UnityEngine;

public sealed class CurvePresetGeneratorWindow : EditorWindow
{
    private const string DefaultFormula = "x";
    private const string DefaultPresetName = "Custom Curve";
    private const int DefaultKeyframeCount = 50;
    private const int MinKeyframeCount = 2;
    private const int MaxKeyframeCount = 200;

    private string _formula = DefaultFormula;
    private string _presetName = DefaultPresetName;
    private string _error;
    private string _statusMessage;
    private AnimationCurve _previewCurve;
    private int _keyframeCount = DefaultKeyframeCount;

    [MenuItem("Tools/Curve Preset Generator")]
    private static void OpenWindow()
    {
        var window = GetWindow<CurvePresetGeneratorWindow>();
        window.titleContent = new GUIContent("Curve Preset Generator");
        window.minSize = new Vector2(400f, 300f);
        window.Show();
    }

    private void OnEnable()
    {
        titleContent = new GUIContent("Curve Preset Generator");
        minSize = new Vector2(400f, 300f);

        if (_previewCurve == null)
        {
            RebuildPreview();
        }
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();

        _formula = EditorGUILayout.TextField("Formula (x: 0→1)", _formula);
        _previewCurve = EditorGUILayout.CurveField("Preview", _previewCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f));
        _presetName = EditorGUILayout.TextField("Preset Name", _presetName);
        _keyframeCount = Mathf.Clamp(EditorGUILayout.IntField("Keyframes", _keyframeCount), MinKeyframeCount, MaxKeyframeCount);

        if (EditorGUI.EndChangeCheck() || GUI.changed)
        {
            RebuildPreview();
        }

        if (!string.IsNullOrEmpty(_error))
        {
            EditorGUILayout.HelpBox(_error, MessageType.Error);
        }

        if (!string.IsNullOrEmpty(_statusMessage))
        {
            EditorGUILayout.HelpBox(_statusMessage, MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(_error) || _previewCurve == null))
        {
            if (GUILayout.Button("Save Preset"))
            {
                CurvePresetWriter.SavePreset(_presetName, _previewCurve);
                AssetDatabase.Refresh();
                _statusMessage = "Saved to Default.curves!";
            }
        }
    }

    private void RebuildPreview()
    {
        Func<float, float> compiled = MathExpressionEvaluator.Compile(_formula, out string compileError);
        if (compiled == null)
        {
            _statusMessage = null;
            _error = compileError;
            return;
        }

        try
        {
            _previewCurve = BuildCurve(compiled, _keyframeCount);
            _error = null;
            _statusMessage = null;
        }
        catch (Exception ex)
        {
            _previewCurve = null;
            _error = ex.Message;
            _statusMessage = null;
        }
    }

    private static AnimationCurve BuildCurve(Func<float, float> compiled, int keyframeCount)
    {
        int sampleCount = Mathf.Clamp(keyframeCount, MinKeyframeCount, MaxKeyframeCount);
        var keyframes = new Keyframe[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = sampleCount == 1 ? 0f : i / (sampleCount - 1f);
            float value = compiled(time);
            float slope = MathExpressionEvaluator.Derivative(compiled, time);
            keyframes[i] = new Keyframe(time, value, slope, slope);
        }

        return new AnimationCurve(keyframes);
    }
}
