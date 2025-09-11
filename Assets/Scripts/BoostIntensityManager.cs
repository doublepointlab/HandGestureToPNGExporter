using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public enum BoostIntensity
{
    x1 = 1,
    x2 = 2,
    x3 = 3,
    x4 = 4,
    x5 = 5
}

public class BoostIntensityManager : MonoBehaviour
{
    [SerializeField] private BoostIntensity boostIntensity = BoostIntensity.x1;
    
    private BoostIntensity previousBoostIntensity = BoostIntensity.x1;
    private bool isProcessingBoostIntensity = false;
    
    // Static list to track all duplicated objects across all instances
    private static List<GameObject> globalDuplicatedObjects = new List<GameObject>();
    
    // Public property for boost intensity
    public BoostIntensity BoostIntensity
    {
        get => boostIntensity;
        set
        {
            if (boostIntensity != value)
            {
                boostIntensity = value;
                ApplyBoostIntensity();
            }
        }
    }
    
    private void OnValidate()
    {
        // Only run in edit mode to avoid issues in play mode
        if (!Application.isPlaying && boostIntensity != previousBoostIntensity && !isProcessingBoostIntensity)
        {
            isProcessingBoostIntensity = true;
            
            // Defer the operation to avoid DestroyImmediate restrictions in OnValidate
            EditorApplication.delayCall += () => {
                previousBoostIntensity = boostIntensity;
                ApplyBoostIntensity();
                isProcessingBoostIntensity = false;
            };
        }
    }
    
    void Start()
    {
        // Automatically recreate duplicates when entering play mode
        if (Application.isPlaying && boostIntensity != BoostIntensity.x1)
        {
            UnityEngine.Debug.Log($"BoostIntensityManager: Auto-recreating duplicates for intensity {boostIntensity} on play mode start");
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
        UnityEngine.Debug.Log($"ApplyBoostIntensity called with intensity: {boostIntensity}");
        
        if (boostIntensity == BoostIntensity.x1)
        {
            // Clean up existing duplicates when setting to x1
            CleanupAllDuplicatedObjects();
            return; // No duplication needed
        }
        
        // Find all SkinnedMeshRenderer objects in the scene (including inactive ones)
        List<GameObject> objectsToDuplicate = FindSkinnedMeshObjects();
        
        // Clean up existing duplicates first
        CleanupAllDuplicatedObjects();
        
        // Small delay to ensure cleanup completes before duplication
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => {
                // Duplicate each object
                foreach (GameObject originalObject in objectsToDuplicate)
                {
                    DuplicateSkinnedMeshObject(originalObject);
                }
                UnityEngine.Debug.Log($"Boost intensity {boostIntensity}: Duplicated {objectsToDuplicate.Count} SkinnedMeshRenderer objects");
            };
        }
        else
        {
            // In play mode, duplicate immediately
            foreach (GameObject originalObject in objectsToDuplicate)
            {
                DuplicateSkinnedMeshObject(originalObject);
            }
            UnityEngine.Debug.Log($"Boost intensity {boostIntensity}: Duplicated {objectsToDuplicate.Count} SkinnedMeshRenderer objects");
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
    
    // Duplicate a SkinnedMeshRenderer object based on boost intensity
    private void DuplicateSkinnedMeshObject(GameObject originalObject)
    {
        int duplicateCount = (int)boostIntensity - 1; // Subtract 1 because original object already exists
        
        for (int i = 0; i < duplicateCount; i++)
        {
            try
            {
                // No offset: all duplicates at the same position as the original
                Vector3 duplicatePosition = originalObject.transform.position;
                
                // Instantiate the duplicate
                GameObject duplicate = Instantiate(originalObject, duplicatePosition, originalObject.transform.rotation, originalObject.transform.parent);
                duplicate.name = originalObject.name + "_Boost_" + (i + 1);
                
                // Mark as don't save in editor to avoid scene pollution
                if (!Application.isPlaying)
                {
                    duplicate.hideFlags = HideFlags.DontSaveInEditor;
                }
                
                // Add to global list
                globalDuplicatedObjects.Add(duplicate);
                
                UnityEngine.Debug.Log($"Created duplicate: {duplicate.name} at position {duplicatePosition}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to duplicate {originalObject.name}: {e.Message}");
            }
        }
    }
    
    
    // Clean up all duplicated objects
    private void CleanupAllDuplicatedObjects()
    {
        UnityEngine.Debug.Log($"CleanupAllDuplicatedObjects called, found {globalDuplicatedObjects.Count} duplicates to clean up");
        
        foreach (GameObject duplicate in globalDuplicatedObjects.ToArray())
        {
            if (duplicate != null)
            {
                UnityEngine.Debug.Log($"Destroying duplicate: {duplicate.name}");
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
        globalDuplicatedObjects.Clear();
    }
    
    // Clean up duplicates and recreate them (useful for play mode transitions)
    public void RefreshDuplicates()
    {
        UnityEngine.Debug.Log($"RefreshDuplicates called with intensity: {boostIntensity}");
        
        // Clean up existing duplicates
        CleanupAllDuplicatedObjects();
        
        // Recreate duplicates if intensity is not x1
        if (boostIntensity != BoostIntensity.x1)
        {
            UnityEngine.Debug.Log($"Recreating duplicates for intensity: {boostIntensity}");
            ApplyBoostIntensity();
        }
    }
    
    // Public method to manually clean up (useful for runtime)
    public void CleanupDuplicates()
    {
        CleanupAllDuplicatedObjects();
    }
    
    // Public method to manually apply boost intensity (useful for runtime)
    public void SetBoostIntensity(BoostIntensity newIntensity)
    {
        boostIntensity = newIntensity;
        ApplyBoostIntensity();
    }
    
    // Context menu for easy testing
    [ContextMenu("Refresh Duplicates")]
    public void RefreshDuplicatesContextMenu()
    {
        RefreshDuplicates();
    }
}