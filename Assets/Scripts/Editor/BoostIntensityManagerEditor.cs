using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BoostIntensityManager))]
public class BoostIntensityManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BoostIntensityManager manager = (BoostIntensityManager)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Boost Intensity Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Apply Boost Intensity"))
        {
            manager.ApplyBoostIntensity();
        }
        
        if (GUILayout.Button("Cleanup Duplicates"))
        {
            manager.CleanupDuplicates();
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Boost Intensity Manager handles duplication of SkinnedMeshRenderer objects. " +
                               "Only works in edit mode to avoid play mode issues.", MessageType.Info);
    }
}