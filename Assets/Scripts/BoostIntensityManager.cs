 using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Manages boost intensity for hand mesh objects with support for light and dark modes.
/// Creates duplicates of SkinnedMeshRenderer objects based on intensity settings.
/// </summary>
public class BoostIntensityManager : MonoBehaviour
{
    #region Constants
    private const int MIN_INTENSITY = 1;
    private const int MAX_INTENSITY = 10;
    private const string BOOST_SUFFIX = "_Boost_";
    private const string LIGHT_MODE = "light";
    private const string DARK_MODE = "dark";
    #endregion

    #region Serialized Fields
    [Header("Intensity Settings")]
    [SerializeField, Range(MIN_INTENSITY, MAX_INTENSITY)] private int intensityLight = MIN_INTENSITY;
    [SerializeField, Range(MIN_INTENSITY, MAX_INTENSITY)] private int intensityDark = MIN_INTENSITY;
    
    [Header("References")]
    [SerializeField] private GlobalFrameRecorder globalFrameRecorder;
    #endregion

    #region Private Fields
    private int previousIntensityLight = MIN_INTENSITY;
    private int previousIntensityDark = MIN_INTENSITY;
    private bool isProcessingBoostIntensity = false;
    #endregion
    
    #region Public Properties
    /// <summary>
    /// Gets or sets the intensity for light mode (1-10)
    /// </summary>
    public int IntensityLight
    {
        get => intensityLight;
        set
        {
            if (intensityLight != value)
            {
                intensityLight = Mathf.Clamp(value, MIN_INTENSITY, MAX_INTENSITY);
                ApplyBoostIntensity();
            }
        }
    }
    
    /// <summary>
    /// Gets or sets the intensity for dark mode (1-10)
    /// </summary>
    public int IntensityDark
    {
        get => intensityDark;
        set
        {
            if (intensityDark != value)
            {
                intensityDark = Mathf.Clamp(value, MIN_INTENSITY, MAX_INTENSITY);
                ApplyBoostIntensity();
            }
        }
    }
    #endregion
    
    #region Unity Lifecycle
    private void OnValidate()
    {
        if (!Application.isPlaying && !isProcessingBoostIntensity)
        {
            bool lightModeChanged = intensityLight != previousIntensityLight;
            bool darkModeChanged = intensityDark != previousIntensityDark;
            
            if (lightModeChanged || darkModeChanged)
            {
                isProcessingBoostIntensity = true;
                
                EditorApplication.delayCall += () => {
                    previousIntensityLight = intensityLight;
                    previousIntensityDark = intensityDark;
                    ApplyBoostIntensity();
                    isProcessingBoostIntensity = false;
                };
            }
        }
    }
    
    private void Start()
    {
        var (currentIntensity, currentMode) = GetCurrentIntensityAndMode();
        
        if (Application.isPlaying && currentIntensity != MIN_INTENSITY)
        {
            Debug.Log($"BoostIntensityManager: Auto-recreating duplicates for {currentMode} intensity {currentIntensity} on play mode start");
            ApplyBoostIntensity();
        }
    }
    
    private void OnDestroy()
    {
        CleanupAllDuplicatedObjects();
    }
    #endregion
    
    #region Helper Methods
    /// <summary>
    /// Gets the current intensity and mode based on dark mode state
    /// </summary>
    private (int intensity, string mode) GetCurrentIntensityAndMode()
    {
        bool isDarkMode = GetDarkModeState();
        int currentIntensity = isDarkMode ? intensityDark : intensityLight;
        string currentMode = isDarkMode ? DARK_MODE : LIGHT_MODE;
        return (currentIntensity, currentMode);
    }
    
    /// <summary>
    /// Checks if the GlobalFrameRecorder is in dark mode
    /// </summary>
    private bool GetDarkModeState()
    {
        return globalFrameRecorder?.IsDarkMode ?? false;
    }
    #endregion

    #region Core Intensity Management
    /// <summary>
    /// Applies boost intensity by duplicating SkinnedMeshRenderer objects
    /// </summary>
    public void ApplyBoostIntensity()
    {
        var (currentIntensity, currentMode) = GetCurrentIntensityAndMode();
        
        Debug.Log($"ApplyBoostIntensity called with {currentMode} mode intensity: {currentIntensity}");
        
        List<GameObject> objectsToDuplicate = FindSkinnedMeshObjects();
        CleanupAllDuplicatedObjects();
        
        ExecuteDuplication(objectsToDuplicate, currentIntensity, currentMode);
    }
    
    /// <summary>
    /// Applies boost intensity without cleanup (for refresh scenarios)
    /// </summary>
    private void ApplyBoostIntensityWithoutCleanup()
    {
        var (currentIntensity, currentMode) = GetCurrentIntensityAndMode();
        
        Debug.Log($"ApplyBoostIntensityWithoutCleanup called with {currentMode} mode intensity: {currentIntensity}");
        
        List<GameObject> objectsToDuplicate = FindSkinnedMeshObjects();
        ExecuteDuplication(objectsToDuplicate, currentIntensity, currentMode);
    }
    
    /// <summary>
    /// Executes the duplication logic with proper timing
    /// </summary>
    private void ExecuteDuplication(List<GameObject> objectsToDuplicate, int currentIntensity, string currentMode)
    {
        if (!Application.isPlaying)
        {
            EditorApplication.delayCall += () => {
                DuplicateObjects(objectsToDuplicate, currentIntensity, currentMode);
            };
        }
        else
        {
            DuplicateObjects(objectsToDuplicate, currentIntensity, currentMode);
        }
    }
    
    /// <summary>
    /// Duplicates the objects and logs the result
    /// </summary>
    private void DuplicateObjects(List<GameObject> objectsToDuplicate, int currentIntensity, string currentMode)
    {
        foreach (GameObject originalObject in objectsToDuplicate)
        {
            DuplicateSkinnedMeshObject(originalObject, currentIntensity, currentMode);
        }
        Debug.Log($"Boost intensity {currentMode} {currentIntensity}: Duplicated {objectsToDuplicate.Count} SkinnedMeshRenderer objects");
    }
    #endregion
    
    #region Object Management
    /// <summary>
    /// Finds all SkinnedMeshRenderer objects in the scene that should be duplicated
    /// </summary>
    private List<GameObject> FindSkinnedMeshObjects()
    {
        List<GameObject> objectsToDuplicate = new List<GameObject>();
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.scene.IsValid() && IsValidHandMeshObject(obj))
            {
                objectsToDuplicate.Add(obj);
            }
        }
        
        return objectsToDuplicate;
    }
    
    /// <summary>
    /// Checks if an object is a valid hand mesh object for duplication
    /// </summary>
    private bool IsValidHandMeshObject(GameObject obj)
    {
        if (obj == null) return false;
        
        SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer == null) return false;
        
        return IsHandMeshObject(obj) && !obj.name.Contains(BOOST_SUFFIX);
    }
    
    /// <summary>
    /// Checks if an object is likely a hand mesh object
    /// </summary>
    private bool IsHandMeshObject(GameObject obj)
    {
        if (obj == null) return false;
        
        string objName = obj.name.ToLower();
        
        bool hasHandKeyword = objName.Contains("hand") || objName.Contains("finger") || 
                             objName.Contains("thumb") || objName.Contains("palm");
        
        bool isNotUI = !objName.Contains("ui") && !objName.Contains("canvas") && 
                       !objName.Contains("button") && !objName.Contains("panel");
        
        bool isNotCameraLight = !objName.Contains("camera") && !objName.Contains("light") && 
                               !objName.Contains("directional") && !objName.Contains("spot");
        
        return hasHandKeyword && isNotUI && isNotCameraLight;
    }
    
    /// <summary>
    /// Duplicates a SkinnedMeshRenderer object based on boost intensity and mode
    /// </summary>
    private void DuplicateSkinnedMeshObject(GameObject originalObject, int intensity, string mode)
    {
        int duplicateCount = intensity - MIN_INTENSITY; // Subtract 1 because original object already exists
        
        for (int i = 0; i < duplicateCount; i++)
        {
            try
            {
                CreateDuplicate(originalObject, mode, i + 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to duplicate {originalObject.name} for {mode} mode: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Creates a single duplicate object
    /// </summary>
    private void CreateDuplicate(GameObject originalObject, string mode, int index)
    {
        Vector3 duplicatePosition = originalObject.transform.position;
        
        GameObject duplicate = Instantiate(originalObject, duplicatePosition, originalObject.transform.rotation, originalObject.transform.parent);
        duplicate.name = $"{originalObject.name}{BOOST_SUFFIX}{mode}_{index}";
        
        if (!Application.isPlaying)
        {
            duplicate.hideFlags = HideFlags.DontSaveInEditor;
        }
        
        Debug.Log($"Created duplicate: {duplicate.name} at position {duplicatePosition}");
    }
    #endregion
    
    
    #region Cleanup Management
    /// <summary>
    /// Cleans up all duplicated objects by finding objects with "boost" in their name
    /// </summary>
    private void CleanupAllDuplicatedObjects(bool deferFromOnValidate = false)
    {
        if (deferFromOnValidate && !Application.isPlaying)
        {
            EditorApplication.delayCall += CleanupAllDuplicatedObjectsInternal;
            return;
        }
        
        CleanupAllDuplicatedObjectsInternal();
    }
    
    /// <summary>
    /// Internal cleanup method that actually performs the destruction
    /// </summary>
    private void CleanupAllDuplicatedObjectsInternal()
    {
        List<GameObject> objectsToDestroy = FindBoostObjects();
        
        foreach (GameObject obj in objectsToDestroy)
        {
            DestroyBoostObject(obj);
        }
        
        if (objectsToDestroy.Count > 0)
        {
            Debug.Log($"Cleaned up {objectsToDestroy.Count} boost objects");
        }
    }
    
    /// <summary>
    /// Finds all objects with "boost" in their name
    /// </summary>
    private List<GameObject> FindBoostObjects()
    {
        List<GameObject> objectsToDestroy = new List<GameObject>();
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject obj in allGameObjects)
        {
            if (obj.scene.IsValid() && obj.name.ToLower().Contains("boost"))
            {
                objectsToDestroy.Add(obj);
            }
        }
        
        return objectsToDestroy;
    }
    
    /// <summary>
    /// Destroys a boost object with appropriate method based on context
    /// </summary>
    private void DestroyBoostObject(GameObject obj)
    {
        if (obj == null) return;
        
        Debug.Log($"Destroying boost object: {obj.name}");
        
        if (!Application.isPlaying)
        {
            DestroyImmediate(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
    #endregion
    
    #region Public API
    /// <summary>
    /// Cleans up duplicates and recreates them (useful for play mode transitions)
    /// </summary>
    public void RefreshDuplicates()
    {
        var (currentIntensity, currentMode) = GetCurrentIntensityAndMode();
        
        Debug.Log($"RefreshDuplicates called with {currentMode} intensity: {currentIntensity}");
        
        CleanupAllDuplicatedObjects();
        
        if (currentIntensity != MIN_INTENSITY)
        {
            Debug.Log($"Recreating duplicates for {currentMode} intensity: {currentIntensity}");
            ApplyBoostIntensity();
        }
    }
    
    /// <summary>
    /// Manually cleans up all boost objects (useful for runtime)
    /// </summary>
    public void CleanupDuplicates()
    {
        CleanupAllDuplicatedObjects();
    }
    
    /// <summary>
    /// Manually sets the light mode intensity (useful for runtime)
    /// </summary>
    public void SetIntensityLight(int newIntensity)
    {
        intensityLight = Mathf.Clamp(newIntensity, MIN_INTENSITY, MAX_INTENSITY);
        ApplyBoostIntensity();
    }
    
    /// <summary>
    /// Manually sets the dark mode intensity (useful for runtime)
    /// </summary>
    public void SetIntensityDark(int newIntensity)
    {
        intensityDark = Mathf.Clamp(newIntensity, MIN_INTENSITY, MAX_INTENSITY);
        ApplyBoostIntensity();
    }
    
    /// <summary>
    /// Gets the current dark mode state from GlobalFrameRecorder
    /// </summary>
    public bool IsDarkMode()
    {
        return globalFrameRecorder?.IsDarkMode ?? false;
    }
    
    /// <summary>
    /// Called when dark mode changes to refresh duplicates
    /// </summary>
    public void OnDarkModeChanged()
    {
        Debug.Log("Dark mode changed, refreshing duplicates");
        RefreshDuplicatesFromOnValidate();
    }
    #endregion

    #region OnValidate Handling
    /// <summary>
    /// Refresh duplicates when called from OnValidate context
    /// </summary>
    private void RefreshDuplicatesFromOnValidate()
    {
        var (currentIntensity, currentMode) = GetCurrentIntensityAndMode();
        
        Debug.Log($"RefreshDuplicatesFromOnValidate called with {currentMode} intensity: {currentIntensity}");
        
        CleanupAllDuplicatedObjects(deferFromOnValidate: true);
        
        if (currentIntensity != MIN_INTENSITY)
        {
            Debug.Log($"Recreating duplicates for {currentMode} intensity: {currentIntensity}");
            EditorApplication.delayCall += ApplyBoostIntensityWithoutCleanup;
        }
    }
    #endregion

    #region Context Menu
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
    #endregion
}