using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FrameExporter : MonoBehaviour
{
    public string exportPath = "Assets/HandsExport/";
    public int width = 1920; // Set the desired width of the exported frame
    public int height = 1080; // Set the desired height of the exported frame
    public Camera cameraToExport; // Declare the camera to export
    
    [HideInInspector] // This attribute hides the selectedPreset field in the Unity inspector
    public ResolutionPreset selectedPreset = ResolutionPreset.res_FullHD_1920x1080; // Default preset
#if UNITY_EDITOR
    public void ExportFrame()
    {
        if (cameraToExport == null)
        {
            Debug.LogError("Camera to export is not set. Please set the camera to export.");
            return;
        }

        // Create a render texture with a depth buffer that supports transparency
        RenderTexture renderTexture = new RenderTexture(width, height, 32, RenderTextureFormat.ARGB32);
        cameraToExport.targetTexture = renderTexture;
        Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);

        // Make sure the camera's clear flags are set to solid color
        // with an alpha of 0 to render the background with full transparency
        CameraClearFlags originalClearFlags = cameraToExport.clearFlags;
        Color originalBackgroundColor = cameraToExport.backgroundColor;

        cameraToExport.clearFlags = CameraClearFlags.SolidColor;
        cameraToExport.backgroundColor = new Color(0, 0, 0, 0); // Transparent background

        cameraToExport.Render();
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture2D.Apply();

        // Restore the original camera settings
        cameraToExport.clearFlags = originalClearFlags;
        cameraToExport.backgroundColor = originalBackgroundColor;

        cameraToExport.targetTexture = null;
        RenderTexture.active = null;
        // Generate a timestamp string to append to the filename
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        // Retrieve the name of the current resolution preset for the filename
        string resolutionPresetName = selectedPreset.ToString();
        // Include GameObject parent and parent of parent names in the filename
        string parentName = this.gameObject.transform.parent != null ? this.gameObject.transform.parent.name : "NoParent";
        string parentOfParentName = this.gameObject.transform.parent != null && this.gameObject.transform.parent.parent != null ? this.gameObject.transform.parent.parent.name : "NoParentOfParent";
        //string fileName = $"{parentOfParentName}_{parentName}_{resolutionPresetName}_{timestamp}.png";
        string fileName = $"{parentOfParentName}_{parentName}_{resolutionPresetName}.png";
        
        

        //Ensure the exportPath exists to avoid DirectoryNotFoundException
        if (!System.IO.Directory.Exists(exportPath))
        {
            System.IO.Directory.CreateDirectory(exportPath);
        }
        //Ensure the exportPath ends with a slash
        // if (!exportPath.EndsWith("/") && !exportPath.EndsWith("\\"))
        // {
        //     exportPath += "/";
        // }
        // Combine the export path and the filename
        string filePath = System.IO.Path.Combine(exportPath, fileName);
        // Debug.Log($"Export Path: {exportPath}");
        // Debug.Log($"File Path: {filePath}");
        // Debug.Log($"File Name: {fileName}");

        byte[] bytes = texture2D.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);

        // Clean up
        DestroyImmediate(renderTexture);
        DestroyImmediate(texture2D);

        // Log a message indicating the save location
        Debug.Log("Frame exported to: " + filePath);
        
    }


#endif
}

#if UNITY_EDITOR
[CustomEditor(typeof(FrameExporter))]
public class FrameExporterEditor : Editor
{
    
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FrameExporter frameExporter = (FrameExporter)target;

        // Preset selection dropdown
        GUILayout.Label("Resolution Preset");
        ResolutionPreset previousPreset = frameExporter.selectedPreset;
        frameExporter.selectedPreset = (ResolutionPreset)EditorGUILayout.EnumPopup("Resolution Preset", frameExporter.selectedPreset);

        // Apply preset immediately when it changes
        if (previousPreset != frameExporter.selectedPreset)
        {
            ApplyResolutionPreset(frameExporter);
        }

        // Export frame button
        if (GUILayout.Button("Export Frame"))
        {
            frameExporter.ExportFrame();
        }
    }

    private void ApplyResolutionPreset(FrameExporter frameExporter)
    {
        switch (frameExporter.selectedPreset)
        {
            case ResolutionPreset.res_HD_1280x720:
                frameExporter.width = 1280;
                frameExporter.height = 720;
                break;
            case ResolutionPreset.res_FullHD_1920x1080:
                frameExporter.width = 1920;
                frameExporter.height = 1080;
                break;
            case ResolutionPreset.res_2K_2560x1440:
                frameExporter.width = 2560;
                frameExporter.height = 1440;
                break;
            case ResolutionPreset.res_4k_3840x2160:
                frameExporter.width = 3840;
                frameExporter.height = 2160;
                break;
            case ResolutionPreset.res_8k_7680x4320:
                frameExporter.width = 7680;
                frameExporter.height = 4320;
                break;
            case ResolutionPreset.res_16k_15360x8640:
                frameExporter.width = 15360;
                frameExporter.height = 8640;
                break;
            case ResolutionPreset.Custom:
                // If custom, do nothing as the user sets the values in the inspector
                break;
        }
        // Apply modifications and update the serialized object
        EditorUtility.SetDirty(frameExporter);
        serializedObject.UpdateIfRequiredOrScript();
    }
}
#endif


public enum ResolutionPreset
{
    res_HD_1280x720,
    res_FullHD_1920x1080,
    res_2K_2560x1440,
    res_4k_3840x2160,
    res_8k_7680x4320,
    res_16k_15360x8640,
    Custom // Allows the user to input custom resolution values
}
