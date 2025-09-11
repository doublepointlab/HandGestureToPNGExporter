using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GlobalFrameRecorder))]
public class GlobalFrameRecorderEditor : Editor
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
    private SerializedProperty isDarkMode;
    private SerializedProperty isRecording;
    private SerializedProperty enableBounceAnimation;
    private SerializedProperty bounceDuration;
    private SerializedProperty bounceIntensity;
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
        isDarkMode = serializedObject.FindProperty("isDarkMode");
        isRecording = serializedObject.FindProperty("isRecording");
        enableBounceAnimation = serializedObject.FindProperty("enableBounceAnimation");
        bounceDuration = serializedObject.FindProperty("bounceDuration");
        bounceIntensity = serializedObject.FindProperty("bounceIntensity");
        moreSettings = serializedObject.FindProperty("moreSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Camera settings
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetCamera, new GUIContent("Target Camera"));
        EditorGUILayout.PropertyField(fadeInOut, new GUIContent("Fade In/Out"));
        EditorGUILayout.Space();

        // Recording settings
        EditorGUILayout.LabelField("Recording Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isRecording, new GUIContent("Is Recording"));
        EditorGUILayout.Space();

        // Export settings
        EditorGUILayout.LabelField("Export Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(exportAsMP4, new GUIContent("Export as MP4"));
        EditorGUILayout.PropertyField(exportAsPNGSequence, new GUIContent("Export as PNG Sequence"));
        EditorGUILayout.PropertyField(exportAsJPGSequence, new GUIContent("Export as JPG Sequence"));
        EditorGUILayout.PropertyField(exportAsGIF, new GUIContent("Export as GIF"));
        EditorGUILayout.PropertyField(exportAsWebM, new GUIContent("Export as WebM"));
        EditorGUILayout.Space();

        // Fade settings
        EditorGUILayout.LabelField("Fade Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isFadingIn, new GUIContent("Is Fading In"));
        EditorGUILayout.PropertyField(isFadingOut, new GUIContent("Is Fading Out"));
        EditorGUILayout.Space();

        // Dark mode settings
        EditorGUILayout.LabelField("Dark Mode Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isDarkMode, new GUIContent("Is Dark Mode"));
        EditorGUILayout.Space();

        // Bounce animation settings
        EditorGUILayout.LabelField("Bounce Animation Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableBounceAnimation, new GUIContent("Enable Bounce Animation"));
        EditorGUILayout.PropertyField(bounceDuration, new GUIContent("Bounce Duration"));
        EditorGUILayout.PropertyField(bounceIntensity, new GUIContent("Bounce Intensity"));
        EditorGUILayout.Space();

        // More settings
        EditorGUILayout.LabelField("Advanced Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moreSettings, new GUIContent("More Settings"), true);

        serializedObject.ApplyModifiedProperties();
    }
}
