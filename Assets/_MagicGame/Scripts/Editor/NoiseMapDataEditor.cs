using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
            GUILayout.Label(noiseData.NoiseTexture, GUILayout.Width(256), GUILayout.Height(256));
        }
    }
}
