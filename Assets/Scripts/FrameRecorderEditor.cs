using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FrameRecorder))]
public class FrameRecorderEditor : Editor
{
    private SerializedProperty targetCamera;
    private SerializedProperty fadeInOut;
    private SerializedProperty exportAsMP4;
    private SerializedProperty exportAsPNGSequence;
    private SerializedProperty exportAsJPGSequence;
    private SerializedProperty exportAsGIF;
    private SerializedProperty exportAsWebM;
    private SerializedProperty isFadingIn;
    private SerializedProperty isFadingOut;
    private SerializedProperty darkModeController;
    private SerializedProperty isRecording;
    private SerializedProperty outputName;
    private SerializedProperty moreSettings;

    void OnEnable()
    {
        targetCamera = serializedObject.FindProperty("targetCamera");
        fadeInOut = serializedObject.FindProperty("fadeInOut");
        exportAsMP4 = serializedObject.FindProperty("exportAsMP4");
        exportAsPNGSequence = serializedObject.FindProperty("exportAsPNGSequence");
        exportAsJPGSequence = serializedObject.FindProperty("exportAsJPGSequence");
        exportAsGIF = serializedObject.FindProperty("exportAsGIF");
        exportAsWebM = serializedObject.FindProperty("exportAsWebM");
        isFadingIn = serializedObject.FindProperty("isFadingIn");
        isFadingOut = serializedObject.FindProperty("isFadingOut");
        darkModeController = serializedObject.FindProperty("darkModeController");
        isRecording = serializedObject.FindProperty("isRecording");
        outputName = serializedObject.FindProperty("outputName");
        moreSettings = serializedObject.FindProperty("moreSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Camera settings
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetCamera, new GUIContent("Target Camera"));

        // Fade settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fadeInOut, new GUIContent("Fade In/Out"));
        EditorGUILayout.PropertyField(isFadingIn, new GUIContent("Is Fading In"));
        EditorGUILayout.PropertyField(isFadingOut, new GUIContent("Is Fading Out"));


        // Dark Mode Controller Reference
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Theme Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(darkModeController, new GUIContent("Dark Mode Controller"));

        // Hand Materials
        SerializedProperty handLight = moreSettings.FindPropertyRelative("_handLight");
        SerializedProperty handDark = moreSettings.FindPropertyRelative("_handDark");
        EditorGUILayout.PropertyField(handLight, new GUIContent("Hand Light Material"));
        EditorGUILayout.PropertyField(handDark, new GUIContent("Hand Dark Material"));

        // Recording settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isRecording, new GUIContent("Is Recording"));
        EditorGUILayout.PropertyField(outputName, new GUIContent("Output Name"));

        // Export settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(exportAsMP4, new GUIContent("Export as MP4"));
        EditorGUILayout.PropertyField(exportAsPNGSequence, new GUIContent("Export as PNG Sequence"));
        EditorGUILayout.PropertyField(exportAsJPGSequence, new GUIContent("Export as JPG Sequence"));
        EditorGUILayout.PropertyField(exportAsGIF, new GUIContent("Export as GIF"));
        EditorGUILayout.PropertyField(exportAsWebM, new GUIContent("Export as WebM"));

        // More Settings (collapsed by default)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("More Settings", EditorStyles.boldLabel);
        
        SerializedProperty backgroundColor = moreSettings.FindPropertyRelative("_backgroundColor");
        SerializedProperty lightBackground = moreSettings.FindPropertyRelative("_lightBackground");
        SerializedProperty outputWidth = moreSettings.FindPropertyRelative("_outputWidth");
        SerializedProperty outputHeight = moreSettings.FindPropertyRelative("_outputHeight");
        SerializedProperty outputScale = moreSettings.FindPropertyRelative("_outputScale");
        SerializedProperty frameRate = moreSettings.FindPropertyRelative("_frameRate");
        SerializedProperty gifFrameRate = moreSettings.FindPropertyRelative("_gifFrameRate");
        SerializedProperty boostIntensity = moreSettings.FindPropertyRelative("_boostIntensity");

        EditorGUILayout.PropertyField(backgroundColor, new GUIContent("Dark"));
        EditorGUILayout.PropertyField(lightBackground, new GUIContent("Light Background"));
        EditorGUILayout.PropertyField(outputWidth, new GUIContent("Output Width"));
        EditorGUILayout.PropertyField(outputHeight, new GUIContent("Output Height"));
        EditorGUILayout.PropertyField(outputScale, new GUIContent("Output Scale"));
        EditorGUILayout.PropertyField(frameRate, new GUIContent("Frame Rate"));
        EditorGUILayout.PropertyField(gifFrameRate, new GUIContent("GIF Frame Rate"));
        
        // Boost Intensity as a slider (Global)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Boost Intensity (Global)", GUILayout.Width(EditorGUIUtility.labelWidth));
        int currentValue = (int)boostIntensity.enumValueIndex + 1; // Convert enum index to 1-10 range
        int newValue = EditorGUILayout.IntSlider(currentValue, 1, 10);
        if (newValue != currentValue)
        {
            boostIntensity.enumValueIndex = newValue - 1; // Convert back to enum index
            
            // Apply globally to all FrameRecorder instances (including inactive ones)
            FrameRecorder[] allFrameRecorders = Resources.FindObjectsOfTypeAll<FrameRecorder>();
            foreach (FrameRecorder frameRecorder in allFrameRecorders)
            {
                if (frameRecorder != null && frameRecorder.gameObject.scene.IsValid())
                {
                    frameRecorder.SetBoostIntensity((FrameRecorder.BoostIntensity)newValue);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
    
    // Force refresh of all FrameRecorder inspectors
    void RefreshAllFrameRecorderInspectors()
    {
        FrameRecorder[] allFrameRecorders = Resources.FindObjectsOfTypeAll<FrameRecorder>();
        foreach (FrameRecorder frameRecorder in allFrameRecorders)
        {
            if (frameRecorder != null && frameRecorder.gameObject.scene.IsValid())
            {
                EditorUtility.SetDirty(frameRecorder);
            }
        }
        
        // Force repaint of all inspectors
        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
    }
}
