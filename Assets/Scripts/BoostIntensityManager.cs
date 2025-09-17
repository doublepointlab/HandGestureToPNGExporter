 using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// Intensity is now an integer from 1-10

public class BoostIntensityManager : MonoBehaviour
{
    [Header("Intensity Settings")]
    [SerializeField, Range(1, 10)] private int intensityLight = 1;
    [SerializeField, Range(1, 10)] private int intensityDark = 1;
    
    [Header("References")]
    [SerializeField] private GlobalFrameRecorder globalFrameRecorder; // Reference to GlobalFrameRecorder
    
    private int previousIntensityLight = 1;
    private int previousIntensityDark = 1;
    private bool isProcessingBoostIntensity = false;
    
    // Static lists to track all duplicated objects across all instances
    private static List<GameObject> globalDuplicatedObjectsLight = new List<GameObject>();
    private static List<GameObject> globalDuplicatedObjectsDark = new List<GameObject>();
    
    // Public property for intensity light (light mode)
    public int IntensityLight
    {
        get => intensityLight;
        set
        {
            if (intensityLight != value)
            {
                intensityLight = Mathf.Clamp(value, 1, 10); // Ensure value is within range
                ApplyBoostIntensity();
            }
        }
    }
    
    // Public property for intensity dark (dark mode)
    public int IntensityDark
    {
        get => intensityDark;
        set
        {
            if (intensityDark != value)
            {
                intensityDark = Mathf.Clamp(value, 1, 10); // Ensure value is within range
                ApplyBoostIntensity();
            }
        }
    }
    
    private void OnValidate()
    {
        // Only run in edit mode to avoid issues in play mode
        if (!Application.isPlaying && !isProcessingBoostIntensity)
        {
            bool lightModeChanged = intensityLight != previousIntensityLight;
            bool darkModeChanged = intensityDark != previousIntensityDark;
            
            if (lightModeChanged || darkModeChanged)
            {
                isProcessingBoostIntensity = true;
                
                // Defer the operation to avoid DestroyImmediate restrictions in OnValidate
                EditorApplication.delayCall += () => {
                    previousIntensityLight = intensityLight;
                    previousIntensityDark = intensityDark;
                    ApplyBoostIntensity();
                    isProcessingBoostIntensity = false;
                };
            }
        }
    }
    
    void Start()
    {
        // Automatically recreate duplicates when entering play mode
        bool isDarkMode = IsDarkMode();
        int currentIntensity = isDarkMode ? intensityDark : intensityLight;
        string currentMode = isDarkMode ? "dark" : "light";
        
        if (Application.isPlaying && currentIntensity != 1)
        {
            UnityEngine.Debug.Log($"BoostIntensityManager: Auto-recreating duplicates for {currentMode} intensity {currentIntensity} on play mode start");
            ApplyBoostIntensity();
        }
    }
    
    private void OnDestroy()
    {
        // Clean up when the manager is destroyed
        CleanupAllDuplicatedObjects();
    }
    
    // Apply boost intensity by duplicating SkinnedMeshRenderer objects
    public void ApplyBoostIntensity()
    {
        // Get current dark mode state
        bool isDarkMode = IsDarkMode();
        int currentIntensity = isDarkMode ? intensityDark : intensityLight;
        string currentMode = isDarkMode ? "dark" : "light";
        
        UnityEngine.Debug.Log($"ApplyBoostIntensity called with {currentMode} mode intensity: {currentIntensity}");
        
        // Find all SkinnedMeshRenderer objects in the scene (including inactive ones)
        List<GameObject> objectsToDuplicate = FindSkinnedMeshObjects();
        
        // Clean up existing duplicates first
        CleanupAllDuplicatedObjects();
        
        // Small delay to ensure cleanup completes before duplication
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => {
                // Duplicate each object for the current mode only
                foreach (GameObject originalObject in objectsToDuplicate)
                {
                    DuplicateSkinnedMeshObject(originalObject, currentIntensity, currentMode);
                }
                UnityEngine.Debug.Log($"Boost intensity {currentMode} {currentIntensity}: Duplicated {objectsToDuplicate.Count} SkinnedMeshRenderer objects");
            };
        }
        else
        {
            // In play mode, duplicate immediately
            foreach (GameObject originalObject in objectsToDuplicate)
            {
                DuplicateSkinnedMeshObject(originalObject, currentIntensity, currentMode);
            }
            UnityEngine.Debug.Log($"Boost intensity {currentMode} {currentIntensity}: Duplicated {objectsToDuplicate.Count} SkinnedMeshRenderer objects");
        }
    }
    
    // Find all SkinnedMeshRenderer objects in the scene
    private List<GameObject> FindSkinnedMeshObjects()
    {
        List<GameObject> objectsToDuplicate = new List<GameObject>();
        
        // Get all GameObjects in the scene, including inactive ones
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject obj in allGameObjects)
        {
            // Only process objects that are in the scene (not prefabs or assets)
            if (obj.scene.IsValid())
            {
                SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    // Check if this is likely a hand/mesh object and not already a duplicate
                    if (IsHandMeshObject(obj) && !obj.name.Contains("_Boost_"))
                    {
                        objectsToDuplicate.Add(obj);
                    }
                }
            }
        }
        
        return objectsToDuplicate;
    }
    
    // Check if an object is likely a hand mesh object
    private bool IsHandMeshObject(GameObject obj)
    {
        if (obj == null) return false;
        
        string objName = obj.name.ToLower();
        
        // Check for hand-related keywords
        bool hasHandKeyword = objName.Contains("hand") || objName.Contains("finger") || 
                             objName.Contains("thumb") || objName.Contains("palm");
        
        // Check for mesh-related keywords
        bool hasMeshKeyword = objName.Contains("mesh") || objName.Contains("model") || 
                             objName.Contains("geometry");
        
        // Check if it's not a UI element
        bool isNotUI = !objName.Contains("ui") && !objName.Contains("canvas") && 
                       !objName.Contains("button") && !objName.Contains("panel");
        
        // Check if it's not a camera or light
        bool isNotCameraLight = !objName.Contains("camera") && !objName.Contains("light") && 
                               !objName.Contains("directional") && !objName.Contains("spot");
        
        // Must have hand keyword and not be UI/camera/light
        return hasHandKeyword && isNotUI && isNotCameraLight;
    }
    
    // Duplicate a SkinnedMeshRenderer object based on boost intensity and mode
    private void DuplicateSkinnedMeshObject(GameObject originalObject, int intensity, string mode)
    {
        int duplicateCount = intensity - 1; // Subtract 1 because original object already exists
        
        for (int i = 0; i < duplicateCount; i++)
        {
            try
            {
                // No offset: all duplicates at the same position as the original
                Vector3 duplicatePosition = originalObject.transform.position;
                
                // Instantiate the duplicate
                GameObject duplicate = Instantiate(originalObject, duplicatePosition, originalObject.transform.rotation, originalObject.transform.parent);
                duplicate.name = originalObject.name + "_Boost_" + mode + "_" + (i + 1);
                
                // Mark as don't save in editor to avoid scene pollution
                if (!Application.isPlaying)
                {
                    duplicate.hideFlags = HideFlags.DontSaveInEditor;
                }
                
                // Add to appropriate global list based on mode
                if (mode == "light")
                {
                    globalDuplicatedObjectsLight.Add(duplicate);
                }
                else if (mode == "dark")
                {
                    globalDuplicatedObjectsDark.Add(duplicate);
                }
                
                UnityEngine.Debug.Log($"Created duplicate: {duplicate.name} at position {duplicatePosition}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to duplicate {originalObject.name} for {mode} mode: {e.Message}");
            }
        }
    }
    
    
    // Clean up all duplicated objects
    private void CleanupAllDuplicatedObjects()
    {
        UnityEngine.Debug.Log($"CleanupAllDuplicatedObjects called, found {globalDuplicatedObjectsLight.Count} light duplicates and {globalDuplicatedObjectsDark.Count} dark duplicates to clean up");
        
        // Clean up light mode duplicates
        foreach (GameObject duplicate in globalDuplicatedObjectsLight.ToArray())
        {
            if (duplicate != null)
            {
                UnityEngine.Debug.Log($"Destroying light duplicate: {duplicate.name}");
                if (!Application.isPlaying)
                {
                    DestroyImmediate(duplicate);
                }
                else
                {
                    Destroy(duplicate);
                }
            }
        }
        globalDuplicatedObjectsLight.Clear();
        
        // Clean up dark mode duplicates
        foreach (GameObject duplicate in globalDuplicatedObjectsDark.ToArray())
        {
            if (duplicate != null)
            {
                UnityEngine.Debug.Log($"Destroying dark duplicate: {duplicate.name}");
                if (!Application.isPlaying)
                {
                    DestroyImmediate(duplicate);
                }
                else
                {
                    Destroy(duplicate);
                }
            }
        }
        globalDuplicatedObjectsDark.Clear();
    }
    
    // Clean up duplicates and recreate them (useful for play mode transitions)
    public void RefreshDuplicates()
    {
        bool isDarkMode = IsDarkMode();
        int currentIntensity = isDarkMode ? intensityDark : intensityLight;
        string currentMode = isDarkMode ? "dark" : "light";
        
        UnityEngine.Debug.Log($"RefreshDuplicates called with {currentMode} intensity: {currentIntensity}");
        
        // Clean up existing duplicates
        CleanupAllDuplicatedObjects();
        
        // Recreate duplicates if current intensity is not 1
        if (currentIntensity != 1)
        {
            UnityEngine.Debug.Log($"Recreating duplicates for {currentMode} intensity: {currentIntensity}");
            ApplyBoostIntensity();
        }
    }
    
    // Public method to manually clean up (useful for runtime)
    public void CleanupDuplicates()
    {
        CleanupAllDuplicatedObjects();
    }
    
    // Public method to manually apply intensity light (useful for runtime)
    public void SetIntensityLight(int newIntensity)
    {
        intensityLight = Mathf.Clamp(newIntensity, 1, 10); // Ensure value is within range
        ApplyBoostIntensity();
    }
    
    // Public method to manually apply intensity dark (useful for runtime)
    public void SetIntensityDark(int newIntensity)
    {
        intensityDark = Mathf.Clamp(newIntensity, 1, 10); // Ensure value is within range
        ApplyBoostIntensity();
    }
    
    // Public method to get current dark mode state from GlobalFrameRecorder
    public bool IsDarkMode()
    {
        if (globalFrameRecorder != null)
        {
            return globalFrameRecorder.IsDarkMode;
        }
        return false; // Default to light mode if no reference
    }
    
    // Public method to refresh duplicates when dark mode changes
    public void OnDarkModeChanged()
    {
        UnityEngine.Debug.Log("Dark mode changed, refreshing duplicates");
        RefreshDuplicates();
    }
    
    // Context menu for easy testing
    [ContextMenu("Refresh Duplicates")]
    public void RefreshDuplicatesContextMenu()
    {
        RefreshDuplicates();
    }
    
    [ContextMenu("Test Dark Mode Change")]
    public void TestDarkModeChange()
    {
        OnDarkModeChanged();
    }
}