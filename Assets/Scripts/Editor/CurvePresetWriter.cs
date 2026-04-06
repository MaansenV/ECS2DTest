using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class CurvePresetWriter
{
    private const string Header = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &1\nMonoBehaviour:\n  m_ObjectHideFlags: 52\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 12322, guid: 0000000000000000e000000000000000, type: 0}\n  m_Name: \n  m_EditorClassIdentifier: \n  m_Presets:";

    public static void SavePreset(string name, AnimationCurve curve)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Preset name is required.", nameof(name));
        }

        if (curve == null)
        {
            throw new ArgumentNullException(nameof(curve));
        }

        string presetsPath = GetPresetsPath();
        string presetsDirectory = Path.GetDirectoryName(presetsPath);
        if (string.IsNullOrEmpty(presetsDirectory))
        {
            throw new InvalidOperationException("Could not determine the presets directory.");
        }

        Directory.CreateDirectory(presetsDirectory);

        List<string> existingEntries = ReadExistingPresetEntries(presetsPath);
        string backupPath = presetsPath + ".bak";

        if (File.Exists(presetsPath))
        {
            File.Copy(presetsPath, backupPath, true);
        }

        existingEntries.Add(BuildPresetEntry(name, curve));
        string yaml = BuildDocument(existingEntries);
        File.WriteAllText(presetsPath, yaml, new UTF8Encoding(false));
    }

    public static string GetPresetsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Unity",
            "Editor-5.x",
            "Preferences",
            "Presets",
            "Default.curves");
    }

    private static List<string> ReadExistingPresetEntries(string presetsPath)
    {
        var entries = new List<string>();

        if (!File.Exists(presetsPath))
        {
            return entries;
        }

        string[] lines = File.ReadAllLines(presetsPath);
        int presetsIndex = Array.FindIndex(lines, line => line == "  m_Presets:");
        if (presetsIndex < 0)
        {
            return entries;
        }

        int startIndex = -1;
        for (int i = presetsIndex + 1; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("  - m_Name: ", StringComparison.Ordinal))
            {
                if (startIndex >= 0)
                {
                    entries.Add(string.Join("\n", lines, startIndex, i - startIndex));
                }

                startIndex = i;
            }
        }

        if (startIndex >= 0)
        {
            entries.Add(string.Join("\n", lines, startIndex, lines.Length - startIndex));
        }

        return entries;
    }

    private static string BuildDocument(IEnumerable<string> entries)
    {
        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (string entry in entries)
        {
            builder.AppendLine(entry.TrimEnd('\r', '\n'));
        }

        return builder.ToString();
    }

    private static string BuildPresetEntry(string name, AnimationCurve curve)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"  - m_Name: {SanitizeName(name)}");
        builder.AppendLine("    m_Curve:");
        builder.AppendLine("      serializedVersion: 2");
        builder.AppendLine("      m_Curve:");

        Keyframe[] keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe key = keys[i];
            builder.AppendLine("      - serializedVersion: 3");
            builder.AppendLine($"        time: {FormatFloat(key.time)}");
            builder.AppendLine($"        value: {FormatFloat(key.value)}");
            builder.AppendLine($"        inSlope: {FormatFloat(key.inTangent)}");
            builder.AppendLine($"        outSlope: {FormatFloat(key.outTangent)}");
            builder.AppendLine("        tangentMode: 0");
            builder.AppendLine($"        weightedMode: {(int)key.weightedMode}");
            builder.AppendLine($"        inWeight: {FormatFloat(key.inWeight)}");
            builder.AppendLine($"        outWeight: {FormatFloat(key.outWeight)}");
        }

        builder.AppendLine("      m_PreInfinity: 2");
        builder.AppendLine("      m_PostInfinity: 2");
        builder.Append("      m_RotationOrder: 4");
        return builder.ToString();
    }

    private static string SanitizeName(string name)
    {
        return name.Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }
}
