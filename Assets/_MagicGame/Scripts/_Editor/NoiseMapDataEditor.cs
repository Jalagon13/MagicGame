using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectWizard;

[CustomEditor(typeof(NoiseMapSO))]
public class NoiseMapDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector with the fields from the ScriptableObject
        DrawDefaultInspector();

        NoiseMapSO noiseData = (NoiseMapSO)target;

        if (GUILayout.Button("Generate Noise"))
        {
            noiseData.GenerateNoiseTexture("Editor Test Seed");
        }

        if (noiseData.NoiseTexture != null)
        {
            // Use EditorGUI.DrawPreviewTexture for better scaling
            GUILayout.Label("Noise Map", EditorStyles.boldLabel);
            Rect rect = GUILayoutUtility.GetRect(512, 512, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawPreviewTexture(rect, noiseData.NoiseTexture, null, ScaleMode.ScaleToFit, 0);
        }
    }
}