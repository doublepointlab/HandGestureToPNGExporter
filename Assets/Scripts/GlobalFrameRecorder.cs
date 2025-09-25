using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class GlobalFrameRecorder : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // The camera to capture frames from
    [SerializeField] private FadeInOut fadeInOut; // Reference to FadeInOut script
    [SerializeField] private BoostIntensityManager boostIntensityManager; // Reference to BoostIntensityManager
    [SerializeField] private MoreSettings moreSettings = new MoreSettings();

    [SerializeField] private bool exportAsMP4 = false; // Export as MP4
    [SerializeField] private bool exportAsPNGSequence = true; // Export as PNG Sequence
    [SerializeField] private bool exportAsJPGSequence = false; // Export as JPG Sequence
    [SerializeField] private bool exportAsGIF = false; // Export as GIF
    [SerializeField] private bool exportAsWebM = false; // Export as WebM with alpha
    [SerializeField] private bool isFadingIn = false; // Flag to check if fading in
    [SerializeField] private bool isFadingOut = false; // Flag to check if fading out
    [SerializeField] private bool isDarkMode = false; // Dark mode toggle
    private bool previousDarkMode = false; // Track previous dark mode state for change detection
    [SerializeField] private bool isLooping = true; // Animation looping toggle
    
    [Header("Icon Settings")]
    [SerializeField] private bool showIcon = true; // Show icon toggle
    [SerializeField] private GameObject iconGameObject; // Reference to icon GameObject
    
    [Header("Bounce Animation Settings")]
    [SerializeField] private bool enableBounceAnimation = true; // Enable bounce animation
    [SerializeField] private float bounceDuration = 0.5f; // Duration of bounce animation
    [SerializeField] private float bounceIntensity = 1.2f; // Intensity of bounce animation

    [SerializeField] public bool isRecording = true; // Flag to check if recording is in progress
    [HideInInspector] public int startFrame = 0; // The frame to start recording
    [HideInInspector] public int endFrame = 100; // The frame to stop recording

    // Current active GameObject with PoseAnimator
    private GameObject currentActiveTarget;
    private string currentOutputName;
    private PoseAnimator currentPoseAnimator;
    
    // Public getter for current output name
    private string CurrentOutputName => currentOutputName;
    
    // Public getter for dark mode state
    public bool IsDarkMode => isDarkMode;
    
    // Public getter for looping state
    public bool IsLooping => isLooping;
    
    private int frameCount = 0; // Counter for frames
    private int actualFrameCount = 0; // Counter for actual saved frames
    private float fadeDuration = 1.0f; // Duration of fade in/out

    [HideInInspector] private Color backgroundColor; // Background color
    [HideInInspector] private int outputWidth; // The width of the output image
    [HideInInspector] private int outputHeight; // The height of the output image
    [HideInInspector] private OutputScale outputScale; // Output scale
    [HideInInspector] public int frameRate; // The frame rate for capturing frames
    [HideInInspector] private int gifFrameRate; // The frame rate for capturing frames for GIF
 

    [System.Serializable]
    private class MoreSettings
    {
        public Color _backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1); // Dark background color
        public Color _lightBackground = new Color(1, 1, 1, 1); // Light background color
        public int _outputWidth = 1280; // The width of the output image
        public int _outputHeight = 720; // The height of the output image
        public OutputScale _outputScale = OutputScale.x2; // Output scale
        public int _frameRate = 60; // The frame rate for capturing frames
        public int _gifFrameRate = 30; // The frame rate for capturing frames for GIF
        public Material _handLight; // Light mode hand material
        public Material _handDark; // Dark mode hand material
    }

    // Enum for output scale
    private enum OutputScale
    {
        x1 = 1,
        x2 = 2,
        x3 = 3,
        x4 = 4
    }


    void Awake()
    {
        
        backgroundColor = moreSettings._backgroundColor;
        outputWidth = moreSettings._outputWidth;
        outputHeight = moreSettings._outputHeight;
        outputScale = moreSettings._outputScale;
        frameRate = moreSettings._frameRate;
        gifFrameRate = moreSettings._gifFrameRate;
        
        // Initialize previous dark mode state
        previousDarkMode = isDarkMode;
        
        // Apply dark mode settings
        ApplyDarkModeSettings();
    }

    // Called when values are changed in the Inspector
    void OnValidate()
    {
        // BOOST INTENSITY LOGIC MOVED TO SEPARATE BoostIntensityManager SCRIPT
        // This prevents boost intensity from running in play mode and causing issues
        
        // Check if dark mode changed and notify BoostIntensityManager
        if (isDarkMode != previousDarkMode)
        {
            previousDarkMode = isDarkMode;
            NotifyBoostIntensityManager();
        }
        
        // Apply dark mode settings
        ApplyDarkModeSettings();
        
        // Apply icon visibility settings
        SetIconVisibility(showIcon);
    }

    // Method to toggle dark mode at runtime
    private void ToggleDarkMode()
    {
        isDarkMode = !isDarkMode;
        previousDarkMode = isDarkMode;
        UpdateOutputName();
        ApplyDarkModeSettings();
        NotifyBoostIntensityManager();
        UnityEngine.Debug.Log($"Dark mode toggled to: {isDarkMode}");
    }
    
    // Public method to toggle dark mode (for external access)
    public void ToggleDarkModePublic()
    {
        ToggleDarkMode();
    }

    // Method to toggle icon visibility at runtime
    private void ToggleIconVisibility()
    {
        showIcon = !showIcon;
        SetIconVisibility(showIcon);
        UpdateOutputName();
        UnityEngine.Debug.Log($"Icon visibility toggled to: {showIcon}");
    }

    // Method to set icon visibility
    private void SetIconVisibility(bool visible)
    {
        showIcon = visible;
        if (iconGameObject != null)
        {
            iconGameObject.SetActive(visible);
        }
        UpdateOutputName();
        UnityEngine.Debug.Log($"Icon visibility set to: {visible}");
    }

    // Method to set dark mode
    private void SetDarkMode(bool darkMode)
    {
        isDarkMode = darkMode;
        previousDarkMode = isDarkMode;
        UpdateOutputName();
        ApplyDarkModeSettings();
        NotifyBoostIntensityManager();
        UnityEngine.Debug.Log($"Dark mode set to: {isDarkMode}");
    }
    
    // Public method to set dark mode (for external access)
    public void SetDarkModePublic(bool darkMode)
    {
        SetDarkMode(darkMode);
    }

    // Method to toggle recording (for external access)
    private void ToggleRecording()
    {
        isRecording = !isRecording;
        UnityEngine.Debug.Log($"Recording {(isRecording ? "STARTED" : "STOPPED")} - Toggle called");
    }
    
    // Method to notify BoostIntensityManager when dark mode changes
    private void NotifyBoostIntensityManager()
    {
        if (boostIntensityManager != null)
        {
            boostIntensityManager.OnDarkModeChanged();
            UnityEngine.Debug.Log($"Notified BoostIntensityManager of dark mode change to: {isDarkMode}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("BoostIntensityManager reference is not set. Cannot notify of dark mode change.");
        }
    }

    // Update output name when dark mode changes
    private void UpdateOutputName()
    {
        if (currentActiveTarget != null)
        {
            currentOutputName = GetOutputNameWithMode(currentActiveTarget.name);
            // Recreate output folder with new name
            CreateOutputFolder();
        }
    }

    // Get bounce animation settings from GlobalFrameRecorder
    public (bool enabled, float duration, float intensity) GetBounceSettings()
    {
        return (enableBounceAnimation, bounceDuration, bounceIntensity);
    }

    void Start()
    {
        // Ensure GIF frame rate is not higher than the main frame rate
        if (gifFrameRate > frameRate)
        {
            UnityEngine.Debug.LogError("GIF frame rate cannot be higher than the main frame rate.");
            return;
        }     
        fadeDuration = FadeInOut.fadeDuration; // Set the fade duration

        if (isRecording)
        {
            UnityEngine.Debug.Log("Global recording started.");

            if(isFadingIn)
            {
                fadeInOut.TriggerFadeIn();
            }
        }
        else{
            UnityEngine.Debug.Log("Not recording. Fade in/out is off. Set isRecording to true if you want to record.");  
        }
        ConfigureCamera(); // Configure the camera settings
        
        // Apply dark mode settings
        ApplyDarkModeSettings();
        
        // Apply icon visibility settings
        SetIconVisibility(showIcon);
        
        StartRecording(); // Start the recording process
    }

    // Register a GameObject with PoseAnimator as the current active target
    public void RegisterActiveTarget(GameObject target)
    {
        if (target == null) return;
        
        currentActiveTarget = target;
        currentOutputName = GetOutputNameWithMode(target.name);
        currentPoseAnimator = target.GetComponent<PoseAnimator>();
        
        // Reset recording for new target
        ResetRecording();
        
        // Create output folder for this target
        CreateOutputFolder();
        
        UnityEngine.Debug.Log($"Registered active target: {currentActiveTarget.name} -> Output name: {currentOutputName} (mode: {(isDarkMode ? "dark" : "light")})");
        UnityEngine.Debug.Log($"PoseAnimator found: {currentPoseAnimator != null}, Durations count: {(currentPoseAnimator?.durations?.Length ?? 0)}");
    }

    // Unregister the current active target
    public void UnregisterActiveTarget()
    {
        if (currentActiveTarget != null)
        {
            UnityEngine.Debug.Log($"Unregistered active target: {currentOutputName}");
        }
        currentActiveTarget = null;
        currentOutputName = null;
        currentPoseAnimator = null;
    }

    // Create the output folder
    void CreateOutputFolder()
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        if (exportAsPNGSequence && !Directory.Exists(Path.Combine("Recordings", "PNG", currentOutputName)))
        {
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", currentOutputName));
        }
        if (exportAsJPGSequence && !Directory.Exists(Path.Combine("Recordings", "JPG", currentOutputName)))
        {
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", currentOutputName));
        }       
    }

    void LateUpdate()
    {
        // Only record if we have an active target
        if (currentActiveTarget == null) return;
        
        // Get the current end frame from the PoseAnimator
        int currentEndFrame = GetCurrentEndFrame();
        
        // Debug logging to understand why CaptureFrame is not being called
        if (frameCount % 60 == 0) // Log every 60 frames (1 second at 60fps)
        {
            UnityEngine.Debug.Log($"DEBUG: frameCount={frameCount}, startFrame={startFrame}, currentEndFrame={currentEndFrame}, isRecording={isRecording}");
        }
        
        // Capture frame if within the recording range
        if (isRecording && frameCount >= startFrame && frameCount <= currentEndFrame)
        {
            UnityEngine.Debug.Log($"Capturing frame {frameCount}");
            CaptureFrame();
        }
        frameCount++;

        if (isRecording)
        {
            // Trigger fade out if required
            if (isFadingOut && frameCount == (currentEndFrame - (int)((fadeDuration) * frameRate)))
            {
                fadeInOut.TriggerFadeOut();
            }
            if (isFadingIn && frameCount == startFrame) 
            {
                fadeInOut.TriggerFadeIn();
            }         
        }
        
        // Debug logging every 30 frames
        if (frameCount % 30 == 0)
        {
            UnityEngine.Debug.Log($"Frame: {frameCount}, Start: {startFrame}, End: {currentEndFrame}, Actual: {actualFrameCount}, Target: {currentOutputName}");
        }
        
        // Log when we start and stop recording
        if (frameCount == startFrame && isRecording)
        {
            UnityEngine.Debug.Log($"Started recording at frame {frameCount}");
        }
        if (frameCount == currentEndFrame && isRecording)
        {
            UnityEngine.Debug.Log($"Finished recording at frame {frameCount}, captured {actualFrameCount} frames");
        }
    }

    // Capture a single frame
    void CaptureFrame()
    {
        int scaledWidth = outputWidth * (int)outputScale;
        int scaledHeight = outputHeight * (int)outputScale;

        // Create a render texture and set it as the target for the camera
        RenderTexture renderTexture = new RenderTexture(scaledWidth, scaledHeight, 24, RenderTextureFormat.ARGB32);
        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        // Read the pixels from the render texture into a texture
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        byte[] bytes = null;
        string filePath = string.Empty;

        // Encode and save the frame as JPG if required
        if (exportAsJPGSequence || exportAsGIF)
        {
            bytes = texture.EncodeToJPG();
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", currentOutputName));
            filePath = Path.Combine("Recordings", "JPG", currentOutputName, $"{currentOutputName}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        }        

        // Encode and save the frame as PNG if required
        if (exportAsPNGSequence || exportAsMP4 || exportAsWebM)
        {
            bytes = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", currentOutputName));
            filePath = Path.Combine("Recordings", "PNG", currentOutputName, $"{currentOutputName}_frame_{actualFrameCount:D}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        } 

        // Encode and save the frame as both JPG and PNG if required
        if (exportAsJPGSequence && exportAsPNGSequence)
        {
            bytes = texture.EncodeToJPG();
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", currentOutputName));
            filePath = Path.Combine("Recordings", "JPG", currentOutputName, $"{currentOutputName}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
            bytes = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", currentOutputName));
            filePath = Path.Combine("Recordings", "PNG", currentOutputName, $"{currentOutputName}_frame_{actualFrameCount:D}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        } 

        // Clean up
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(texture);

        actualFrameCount++;        
    }

    // Start the recording process
    private void StartRecording()
    {
        frameCount = 0;
        actualFrameCount = 0;
        Time.captureFramerate = frameRate;        
        Application.targetFrameRate = frameRate;
    }

    // Set the end frame for the current recording session
    public void SetEndFrame(int endFrame)
    {
        this.endFrame = endFrame;
    }

    // Get the current end frame from the active PoseAnimator
    public int GetCurrentEndFrame()
    {
        if (currentPoseAnimator != null)
        {
            // Calculate total frames from the PoseAnimator's durations
            float totalFrames = 0;
            if (currentPoseAnimator.durations != null && currentPoseAnimator.durations.Length > 0)
            {
                foreach (var duration in currentPoseAnimator.durations)
                {
                    totalFrames += Mathf.RoundToInt(duration * frameRate);
                }
                UnityEngine.Debug.Log($"GetCurrentEndFrame: Calculated {totalFrames} frames from {currentPoseAnimator.durations.Length} durations");
                return (int)totalFrames;
            }
            else
            {
                UnityEngine.Debug.LogWarning("GetCurrentEndFrame: currentPoseAnimator.durations is null or empty");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("GetCurrentEndFrame: currentPoseAnimator is null");
        }
        return endFrame; // Fallback to stored end frame
    }

    // Generate output name with dark/light mode suffix and icon suffix
    private string GetOutputNameWithMode(string baseName)
    {
        string modeSuffix = isDarkMode ? "_dark" : "_light";
        string iconSuffix = showIcon ? "_icon" : "";
        return baseName + modeSuffix + iconSuffix;
    }

    // Reset recording for new target
    private void ResetRecording()
    {
        frameCount = 0;
        actualFrameCount = 0;
        startFrame = 0;
        Time.captureFramerate = frameRate;        
        Application.targetFrameRate = frameRate;
    }

    // Handle application quit event
    void OnApplicationQuit()
    {
        if (isRecording)
        {
            if (exportAsMP4)
            {            
                ExportToMP4(); // Export to MP4
            }
            if (exportAsGIF)
            {   
                ExportToGIF(); // Export to GIF
            }
            if (exportAsWebM)
            {
                ExportToWebM(); // Export to WebM with alpha
            }
            OpenOutputFolder(); // Open the output folder
        }
    }

    // Handle object destruction
    void OnDestroy()
    {
        // Cleanup handled automatically by Unity
    }

    // Open the output folder in the file explorer and select the exported file
    // Priority: MP4 > GIF > WebM
    void OpenOutputFolder()
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        string filePath = string.Empty;
        
        // Check export flags in priority order
        if (exportAsMP4)
        {
            filePath = Path.Combine("Recordings", "MP4", $"{currentOutputName}.mp4");
        }
        else if (exportAsGIF)
        {
            filePath = Path.Combine("Recordings", "GIF", $"{currentOutputName}.gif");
        }
        else if (exportAsWebM)
        {
            filePath = Path.Combine("Recordings", "WebM", $"{currentOutputName}.webm");
        }
        else
        {
            // If no exports are enabled, default to PNG folder
            filePath = Path.Combine("Recordings", "PNG", currentOutputName);
        }
        
        #if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        #elif UNITY_EDITOR_OSX
            Process.Start("open", $"-R \"{filePath}\"");
        #endif
    }

    // Export the recorded frames to MP4
    void ExportToMP4()
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "PNG", currentOutputName, $"{currentOutputName}_frame_%d.png");
        string outputFilePath = Path.Combine("Recordings", "MP4", $"{currentOutputName}.mp4");
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        ProcessStartInfo processStartInfo = new ProcessStartInfo(ffmpegPath)
        {
            Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p \"{outputFilePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processStartInfo))
        {
            process.WaitForExit();
            while (!File.Exists(outputFilePath))
            {
                System.Threading.Thread.Sleep(100); // Wait for 100 milliseconds before checking again
            }
            UnityEngine.Debug.Log($"MP4 export completed. Total frames: {actualFrameCount}, Total time: {actualFrameCount / (float)frameRate} seconds.");
        }
        if (exportAsMP4 && !exportAsPNGSequence && !exportAsWebM)
        {
            DeleteFiles("PNG", "*.png"); // Delete PNG files if not required
        }
    }

    // Export the recorded frames to GIF
    void ExportToGIF()
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "JPG", currentOutputName, $"{currentOutputName}_frame_%d.jpg");
        string outputFilePath = Path.Combine("Recordings", "GIF", $"{currentOutputName}.gif");
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

        ProcessStartInfo processStartInfo = new ProcessStartInfo(ffmpegPath)
        {
            Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -vf \"fps={gifFrameRate}\" \"{outputFilePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processStartInfo))
        {
            process.WaitForExit();
            while (!File.Exists(outputFilePath))
            {
                System.Threading.Thread.Sleep(100); // Wait for 100 milliseconds before checking again
            }
            UnityEngine.Debug.Log($"GIF export completed. Total frames: {actualFrameCount}, Total time: {actualFrameCount / (float)gifFrameRate} seconds.");
        }

        if (exportAsGIF && !exportAsJPGSequence)
        {
            DeleteFiles("JPG", "*.jpg"); // Delete JPG files if not required
        }
    }

    // Export the recorded frames to WebM with alpha
    void ExportToWebM()
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "PNG", currentOutputName, $"{currentOutputName}_frame_%d.png");
        string outputFilePath = Path.Combine("Recordings", "WebM", $"{currentOutputName}.webm");
        if (File.Exists(outputFilePath))
        {
            File.Delete(outputFilePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));

        ProcessStartInfo processStartInfo = new ProcessStartInfo(ffmpegPath)
        {
            Arguments = $"-framerate {frameRate} -start_number 0 -i \"{inputPattern}\" -c:v libvpx-vp9 -pix_fmt yuva420p -lossless 1 \"{outputFilePath}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (Process process = Process.Start(processStartInfo))
        {
            process.WaitForExit();
            while (!File.Exists(outputFilePath))
            {
                System.Threading.Thread.Sleep(100); // Wait for 100 milliseconds before checking again
            }
            UnityEngine.Debug.Log($"WebM export completed. Total frames: {actualFrameCount}, Total time: {actualFrameCount / (float)frameRate} seconds.");
        }

        if (exportAsWebM && !exportAsPNGSequence)
        {
            DeleteFiles("PNG", "*.png"); // Delete PNG files if not required
        }
    }


    // Apply global dark mode settings to all FrameRecorder instances (including inactive ones)
    private static void ApplyGlobalDarkModeSettings()
    {
        // Find all GlobalFrameRecorder instances in the scene, including inactive ones
        GlobalFrameRecorder[] allFrameRecorders = Resources.FindObjectsOfTypeAll<GlobalFrameRecorder>();
        
        foreach (GlobalFrameRecorder frameRecorder in allFrameRecorders)
        {
            if (frameRecorder != null && frameRecorder.gameObject.scene.IsValid())
            {
                frameRecorder.ApplyDarkModeSettings();
            }
        }
    }


    // Apply dark mode settings
    void ApplyDarkModeSettings()
    {
        // Update background color based on dark mode
        if (isDarkMode)
        {
            backgroundColor = moreSettings._backgroundColor;
        }
        else
        {
            backgroundColor = moreSettings._lightBackground;
        }
        
        // Apply material to all hand meshes globally
        ApplyMaterialToAllHandMeshes();
        
        // Update camera background color
        if (targetCamera != null)
        {
            targetCamera.backgroundColor = backgroundColor;
        }
    }

    // Apply material to all hand meshes globally (including inactive ones)
    void ApplyMaterialToAllHandMeshes()
    {
        // Find all GameObjects in the scene, including inactive ones
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        
        foreach (GameObject obj in allGameObjects)
        {
            // Only process objects that are in the scene (not prefabs or assets)
            if (obj.scene.IsValid())
            {
                SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                if (skinnedMeshRenderer != null)
                {
                    // Check if this is likely a hand mesh (contains "hand" in name or has hand-related components)
                    if (IsHandMesh(obj))
                    {
                        ApplyMaterialToHandMesh(obj);
                    }
                }
            }
        }
        
    }


    // Check if a GameObject is likely a hand mesh
    bool IsHandMesh(GameObject obj)
    {
        if (obj == null) return false;
        
        string objectName = obj.name.ToLower();
        
        // Check for hand-related keywords in the name
        if (objectName.Contains("hand") || objectName.Contains("finger") || objectName.Contains("palm") || 
            objectName.Contains("arm") || objectName.Contains("body") || objectName.Contains("mesh"))
        {
            return true;
        }
        
        // Check for hand-related components
        if (obj.GetComponent<HandPoseAnimator>() != null || obj.GetComponent<PoseAnimator>() != null)
        {
            return true;
        }
        
        // Check if it has a SkinnedMeshRenderer (likely a character/body part)
        SkinnedMeshRenderer skinnedMeshRenderer = obj.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            // Additional check: make sure it's not a UI element or other non-character mesh
            if (!objectName.Contains("ui") && !objectName.Contains("canvas") && !objectName.Contains("panel"))
            {
                return true;
            }
        }
        
        return false;
    }

    // Apply material to a specific hand mesh
    void ApplyMaterialToHandMesh(GameObject handMeshObject)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = handMeshObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            if (isDarkMode && moreSettings._handLight != null)
            {
                skinnedMeshRenderer.material = moreSettings._handLight;
            }
            else if (!isDarkMode && moreSettings._handDark != null)
            {
                skinnedMeshRenderer.material = moreSettings._handDark;
            }
        }
    }

    // Configure the camera settings
    void ConfigureCamera()
    {
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = backgroundColor;
    }

    // Delete files in a subfolder with a specific pattern
    void DeleteFiles(string subFolder, string pattern)
    {
        if (string.IsNullOrEmpty(currentOutputName)) return;
        
        string subFolderOutputPath = Path.Combine("Recordings", subFolder, currentOutputName);
        if (Directory.Exists(subFolderOutputPath))
        {
            string[] files = Directory.GetFiles(subFolderOutputPath, pattern);
            foreach (string file in files)
            {
                File.Delete(file);
            }
            Directory.Delete(subFolderOutputPath);
        }
        string subFolderPath = Path.Combine("Recordings", subFolder);
        // Check if the subfolder is empty and delete it if it is
        if (Directory.Exists(subFolderPath) && Directory.GetFiles(subFolderPath).Length == 0)
        {
            Directory.Delete(subFolderPath);
        }
    }
}
