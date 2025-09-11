using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class FrameRecorder : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // The camera to capture frames from
    [SerializeField] private FadeInOut fadeInOut; // Reference to FadeInOut script
    [SerializeField] private MoreSettings moreSettings = new MoreSettings();
    private BoostIntensity previousBoostIntensity = BoostIntensity.x1; // Track previous boost intensity for change detection

    [SerializeField] private bool exportAsMP4 = false; // Export as MP4
    [SerializeField] private bool exportAsPNGSequence = true; // Export as PNG Sequence
    [SerializeField] private bool exportAsJPGSequence = false; // Export as JPG Sequence
    [SerializeField] private bool exportAsGIF = false; // Export as GIF
    [SerializeField] private bool exportAsWebM = false; // Export as WebM with alpha
    [SerializeField] private bool isFadingIn = false; // Flag to check if fading in
    [SerializeField] private bool isFadingOut = false; // Flag to check if fading out
    [SerializeField] private DarkModeController darkModeController; // Reference to DarkModeController
    private static BoostIntensity globalBoostIntensity = BoostIntensity.x1; // Global boost intensity state
    private static List<GameObject> globalDuplicatedHands = new List<GameObject>(); // Global list to track duplicated hands

    public bool isRecording = true; // Flag to check if recording is in progress
    [HideInInspector] public int startFrame = 0; // The frame to start recording
    [HideInInspector] public int endFrame = 100; // The frame to stop recording

    [SerializeField] private string outputName; // The folder to save the frames
    private int frameCount = 0; // Counter for frames
    private int actualFrameCount = 0; // Counter for actual saved frames
    private float fadeDuration = 1.0f; // Duration of fade in/out
    private int frameInterval = 1; // Frame interval for continuous recording

    [HideInInspector] private Color backgroundColor; // Background color
    [HideInInspector] private int outputWidth; // The width of the output image
    [HideInInspector] private int outputHeight; // The height of the output image
    [HideInInspector] private OutputScale outputScale; // Output scale
    [HideInInspector] public int frameRate; // The frame rate for capturing frames
    [HideInInspector] private int gifFrameRate; // The frame rate for capturing frames for GIF
    [HideInInspector] private BoostIntensity boostIntensity; // Boost intensity for hand duplication

    [System.Serializable]
    public class MoreSettings
    {
        public Color _backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1); // Dark background color
        public Color _lightBackground = new Color(1, 1, 1, 1); // Light background color
        public int _outputWidth = 1280; // The width of the output image
        public int _outputHeight = 720; // The height of the output image
        public OutputScale _outputScale = OutputScale.x2; // Output scale
        public int _frameRate = 60; // The frame rate for capturing frames
        public int _gifFrameRate = 30; // The frame rate for capturing frames for GIF
        public BoostIntensity _boostIntensity = BoostIntensity.x1; // Boost intensity: Duplicates hand mesh multiple times for enhanced visual impact
        public Material _handLight; // Light mode hand material
        public Material _handDark; // Dark mode hand material
    }

    // Enum for output scale
    public enum OutputScale
    {
        x1 = 1,
        x2 = 2,
        x3 = 3,
        x4 = 4
    }

    // Enum for boost intensity
    public enum BoostIntensity
    {
        x1 = 1,
        x2 = 2,
        x3 = 3,
        x4 = 4,
        x5 = 5,
        x6 = 6,
        x7 = 7,
        x8 = 8,
        x9 = 9,
        x10 = 10
    }

    void Awake()
    {
        backgroundColor = moreSettings._backgroundColor;
        outputWidth = moreSettings._outputWidth;
        outputHeight = moreSettings._outputHeight;
        outputScale = moreSettings._outputScale;
        frameRate = moreSettings._frameRate;
        gifFrameRate = moreSettings._gifFrameRate;
        boostIntensity = moreSettings._boostIntensity;
        
        // Apply dark mode settings
        ApplyDarkModeSettings();
    }

    // Called when values are changed in the Inspector
    void OnValidate()
    {
        // Only run duplication in edit mode (not play mode)
        if (!Application.isPlaying)
        {
        // Check if boost intensity has changed
        if (moreSettings._boostIntensity != previousBoostIntensity)
        {
            previousBoostIntensity = moreSettings._boostIntensity;
            if (globalBoostIntensity != moreSettings._boostIntensity)
            {
                globalBoostIntensity = moreSettings._boostIntensity;
                ApplyGlobalBoostIntensity();
            }
        }
        
        // Apply dark mode settings
        ApplyDarkModeSettings();
        }
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
        frameInterval = endFrame - startFrame;

        if (isRecording)
        {
            UnityEngine.Debug.Log("Recording started.");

            if(isFadingIn)
            {
                fadeInOut.TriggerFadeIn();
            }
        }
        else{
            UnityEngine.Debug.Log("Not recording. Fade in/out is off. Set isRecording to true if you want to record.");  
        }
        CreateOutputFolder();
        ConfigureCamera(); // Configure the camera settings
        previousBoostIntensity = moreSettings._boostIntensity; // Initialize previous boost intensity
        
        // Subscribe to dark mode changes
        SubscribeToDarkModeChanges();
        
        StartRecording(); // Start the recording process
        
    }

    // Create the output folder
    void CreateOutputFolder()
    {
        if (exportAsPNGSequence && !Directory.Exists(Path.Combine("Recordings", "PNG", outputName)))
        {
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", outputName));
        }
        if (exportAsJPGSequence && !Directory.Exists(Path.Combine("Recordings", "JPG", outputName)))
        {
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", outputName));
        }       
    }

    void LateUpdate()
    {
        // Capture frame if within the recording range
        if (isRecording && frameCount >= startFrame && frameCount <= endFrame)
        {
            CaptureFrame();
        }
        frameCount++;

        if (isRecording)
        {
            // Update frame range for continuous playback
            if (frameCount > endFrame)
            {            
                startFrame = endFrame + 1;
                endFrame += frameInterval; 
                frameCount = startFrame;
            }

            // Trigger fade out if required
            if (isFadingOut && frameCount == (endFrame - (int)((fadeDuration) * frameRate)))
            {
                fadeInOut.TriggerFadeOut();
            }
            if (isFadingIn && frameCount == startFrame) 
            {
                fadeInOut.TriggerFadeIn();
            }         
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
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", outputName));
            filePath = Path.Combine("Recordings", "JPG", outputName, $"{outputName}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        }        

        // Encode and save the frame as PNG if required
        if (exportAsPNGSequence || exportAsMP4 || exportAsWebM)
        {
            bytes = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", outputName));
            filePath = Path.Combine("Recordings", "PNG", outputName, $"{outputName}_frame_{actualFrameCount:D}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        } 

        // Encode and save the frame as both JPG and PNG if required
        if (exportAsJPGSequence && exportAsPNGSequence)
        {
            bytes = texture.EncodeToJPG();
            Directory.CreateDirectory(Path.Combine("Recordings", "JPG", outputName));
            filePath = Path.Combine("Recordings", "JPG", outputName, $"{outputName}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
            bytes = texture.EncodeToPNG();
            Directory.CreateDirectory(Path.Combine("Recordings", "PNG", outputName));
            filePath = Path.Combine("Recordings", "PNG", outputName, $"{outputName}_frame_{actualFrameCount:D}.png");
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
    public void StartRecording()
    {
        frameCount = 0;
        actualFrameCount = 0;
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
        CleanupGlobalDuplicatedHands(); // Clean up global duplicated hands
    }

    // Handle object destruction
    void OnDestroy()
    {
        // Unsubscribe from dark mode changes
        UnsubscribeFromDarkModeChanges();
        // Cleanup handled automatically by Unity
    }

    // Subscribe to dark mode changes
    void SubscribeToDarkModeChanges()
    {
        if (darkModeController != null)
        {
            darkModeController.OnDarkModeChanged += OnDarkModeStateChanged;
        }
    }

    // Unsubscribe from dark mode changes
    void UnsubscribeFromDarkModeChanges()
    {
        if (darkModeController != null)
        {
            darkModeController.OnDarkModeChanged -= OnDarkModeStateChanged;
        }
    }

    // Handle dark mode state changes
    void OnDarkModeStateChanged(bool isDarkMode)
    {
        // Update references accordingly
        ApplyDarkModeSettings();
    }

    // Open the output folder in the file explorer and select the exported file
    // Priority: MP4 > GIF > WebM
    void OpenOutputFolder()
    {
        string filePath = string.Empty;
        
        // Check export flags in priority order
        if (exportAsMP4)
        {
            filePath = Path.Combine("Recordings", "MP4", $"{outputName}.mp4");
        }
        else if (exportAsGIF)
        {
            filePath = Path.Combine("Recordings", "GIF", $"{outputName}.gif");
        }
        else if (exportAsWebM)
        {
            filePath = Path.Combine("Recordings", "WebM", $"{outputName}.webm");
        }
        else
        {
            // If no exports are enabled, default to PNG folder
            filePath = Path.Combine("Recordings", "PNG", outputName);
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
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "PNG", outputName, $"{outputName}_frame_%d.png");
        string outputFilePath = Path.Combine("Recordings", "MP4", $"{outputName}.mp4");
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
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "JPG", outputName, $"{outputName}_frame_%d.jpg");
        string outputFilePath = Path.Combine("Recordings", "GIF", $"{outputName}.gif");
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
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine("Recordings", "PNG", outputName, $"{outputName}_frame_%d.png");
        string outputFilePath = Path.Combine("Recordings", "WebM", $"{outputName}.webm");
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



    // Method to change boost intensity at runtime
    public void SetBoostIntensity(BoostIntensity newIntensity)
    {
        if (moreSettings._boostIntensity != newIntensity)
        {
            moreSettings._boostIntensity = newIntensity;
            globalBoostIntensity = newIntensity;
            ApplyGlobalBoostIntensity();
        }
    }


    // Apply global dark mode settings to all FrameRecorder instances (including inactive ones)
    public static void ApplyGlobalDarkModeSettings()
    {
        // Find all FrameRecorder instances in the scene, including inactive ones
        FrameRecorder[] allFrameRecorders = Resources.FindObjectsOfTypeAll<FrameRecorder>();
        
        foreach (FrameRecorder frameRecorder in allFrameRecorders)
        {
            if (frameRecorder != null && frameRecorder.gameObject.scene.IsValid())
            {
                frameRecorder.ApplyDarkModeSettings();
            }
        }
    }

    // Apply global boost intensity to duplicate all hand meshes
    static void ApplyGlobalBoostIntensity()
    {
        // Clean up existing duplicates first
        CleanupGlobalDuplicatedHands();
        
        if (globalBoostIntensity == BoostIntensity.x1)
        {
            return; // No duplication needed
        }

        // Find all hand meshes in the scene (including inactive ones)
        List<GameObject> originalHandMeshes = new List<GameObject>();
        
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
                    // Check if this is likely a hand mesh and not already a duplicate
                    if (IsHandMeshStatic(obj) && !obj.name.Contains("_Boost_"))
                    {
                        originalHandMeshes.Add(obj);
                    }
                }
            }
        }

        // Duplicate each original hand mesh
        foreach (GameObject originalHand in originalHandMeshes)
        {
            DuplicateHandMesh(originalHand);
        }
        
        UnityEngine.Debug.Log($"Global boost intensity {globalBoostIntensity}: Duplicated {originalHandMeshes.Count} hand meshes");
    }

    // Duplicate a specific hand mesh based on boost intensity
    static void DuplicateHandMesh(GameObject originalHand)
    {
        int duplicateCount = (int)globalBoostIntensity - 1; // Subtract 1 because original hand already exists
        float spacing = 2.0f; // Spacing between duplicated hands

        for (int i = 0; i < duplicateCount; i++)
        {
            try
            {
                // Calculate position offset for this duplicate
                Vector3 offset = new Vector3((i + 1) * spacing, 0, 0);
                Vector3 duplicatePosition = originalHand.transform.position + offset;

                // Instantiate the duplicate
                GameObject duplicate = Instantiate(originalHand, duplicatePosition, originalHand.transform.rotation, originalHand.transform.parent);
                duplicate.name = originalHand.name + "_Boost_" + (i + 1);

                // Disable animation components to prevent conflicts
                HandPoseAnimator handPoseAnimator = duplicate.GetComponent<HandPoseAnimator>();
                if (handPoseAnimator != null)
                {
                    handPoseAnimator.enabled = false;
                }

                PoseAnimator poseAnimator = duplicate.GetComponent<PoseAnimator>();
                if (poseAnimator != null)
                {
                    poseAnimator.enabled = false;
                }

                // Disable any other animation-related components
                Animator animator = duplicate.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                // In edit mode, mark the duplicate as not editable to prevent further issues
                if (!Application.isPlaying)
                {
                    duplicate.hideFlags = HideFlags.DontSaveInEditor;
                }

                // Add to global tracking list
                globalDuplicatedHands.Add(duplicate);

                UnityEngine.Debug.Log($"Duplicated hand: {duplicate.name} at position {duplicatePosition}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to duplicate hand {i + 1}: {e.Message}");
            }
        }
    }

    // Clean up all global duplicated hand gameobjects
    static void CleanupGlobalDuplicatedHands()
    {
        foreach (GameObject duplicate in globalDuplicatedHands)
        {
            if (duplicate != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(duplicate);
                }
                else
                {
                    DestroyImmediate(duplicate);
                }
            }
        }
        globalDuplicatedHands.Clear();
    }

    // Apply dark mode settings
    void ApplyDarkModeSettings()
    {
        // Get dark mode state from DarkModeController reference
        bool isDarkMode = darkModeController != null ? darkModeController.IsDarkMode : false;
        
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
        
        // Also apply materials to global duplicated hands
        foreach (GameObject duplicate in globalDuplicatedHands)
        {
            if (duplicate != null)
            {
                ApplyMaterialToHandMesh(duplicate);
            }
        }
    }

    // Check if a GameObject is likely a hand mesh (static version)
    static bool IsHandMeshStatic(GameObject obj)
    {
        string objectName = obj.name.ToLower();
        
        // Check for hand-related keywords in the name
        if (objectName.Contains("hand") || objectName.Contains("finger") || objectName.Contains("palm"))
        {
            return true;
        }
        
        // Check for hand-related components
        if (obj.GetComponent<HandPoseAnimator>() != null || obj.GetComponent<PoseAnimator>() != null)
        {
            return true;
        }
        
        return false;
    }

    // Check if a GameObject is likely a hand mesh (instance version)
    bool IsHandMesh(GameObject obj)
    {
        return IsHandMeshStatic(obj);
    }

    // Apply material to a specific hand mesh
    void ApplyMaterialToHandMesh(GameObject handMeshObject)
    {
        SkinnedMeshRenderer skinnedMeshRenderer = handMeshObject.GetComponent<SkinnedMeshRenderer>();
        if (skinnedMeshRenderer != null)
        {
            // Get dark mode state from DarkModeController reference
            bool isDarkMode = darkModeController != null ? darkModeController.IsDarkMode : false;
            
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
        string subFolderOutputPath = Path.Combine("Recordings", subFolder, outputName);
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