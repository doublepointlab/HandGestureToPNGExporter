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
    [Header("Hand Mesh")]
    [SerializeField] private GameObject handGameObject; // The hand gameobject to duplicate
    [SerializeField] private BoostIntensity boostIntensity = BoostIntensity.x1; // Boost intensity: Duplicates hand gameobject multiple times for enhanced visual impact
    private BoostIntensity previousBoostIntensity = BoostIntensity.x1; // Track previous boost intensity for change detection

    [SerializeField] private bool exportAsMP4 = false; // Export as MP4
    [SerializeField] private bool exportAsPNGSequence = true; // Export as PNG Sequence
    [SerializeField] private bool exportAsJPGSequence = false; // Export as JPG Sequence
    [SerializeField] private bool exportAsGIF = false; // Export as GIF
    [SerializeField] private bool exportAsWebM = false; // Export as WebM with alpha
    [SerializeField] private bool isFadingIn = false; // Flag to check if fading in
    [SerializeField] private bool isFadingOut = false; // Flag to check if fading out

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
    [HideInInspector] private List<GameObject> duplicatedHands = new List<GameObject>(); // List to track duplicated hand gameobjects

    [System.Serializable]
    public class MoreSettings
    {
        public Color _backgroundColor = new Color(0, 0, 0, 0); // Background color
        public int _outputWidth = 1280; // The width of the output image
        public int _outputHeight = 720; // The height of the output image
        public OutputScale _outputScale = OutputScale.x2; // Output scale
        public int _frameRate = 60; // The frame rate for capturing frames
        public int _gifFrameRate = 30; // The frame rate for capturing frames for GIF
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
        x5 = 5
    }

    void Awake()
    {
        backgroundColor = moreSettings._backgroundColor;
        outputWidth = moreSettings._outputWidth;
        outputHeight = moreSettings._outputHeight;
        outputScale = moreSettings._outputScale;
        frameRate = moreSettings._frameRate;
        gifFrameRate = moreSettings._gifFrameRate;
    }

    // Called when values are changed in the Inspector
    void OnValidate()
    {
        // Only run duplication in edit mode (not play mode)
        if (!Application.isPlaying)
        {
            // Check if boost intensity has changed
            if (boostIntensity != previousBoostIntensity)
            {
                // Defer cleanup to avoid DestroyImmediate restrictions in OnValidate
                EditorApplication.delayCall += () => {
                    CleanupDuplicatedHands();
                    previousBoostIntensity = boostIntensity;
                    DuplicateHands();
                };
            }
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
        previousBoostIntensity = boostIntensity; // Initialize previous boost intensity
        DuplicateHands(); // Duplicate hands based on boost intensity
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
        CleanupDuplicatedHands(); // Clean up duplicated hands
    }

    // Handle object destruction
    void OnDestroy()
    {
        CleanupDuplicatedHands(); // Clean up duplicated hands
    }

    // Open the output folder in the file explorer and select the file outputname .gif
    void OpenOutputFolder()
    {
        string filePath = Path.Combine("Recordings", "GIF", $"{outputName}.gif");
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

    // Duplicate hand gameobjects based on boost intensity
    void DuplicateHands()
    {
        if (handGameObject == null)
        {
            UnityEngine.Debug.LogWarning("Hand GameObject is not assigned. Cannot duplicate hands.");
            return;
        }

        if (boostIntensity == BoostIntensity.x1)
        {
            return; // No duplication needed
        }

        // Prevent duplication if this is already a duplicate
        if (handGameObject.name.Contains("_Boost_"))
        {
            UnityEngine.Debug.LogWarning("Cannot duplicate a hand that is already a duplicate. Skipping duplication.");
            return;
        }

        // Clean up any existing duplicates first
        CleanupDuplicatedHands();

        int duplicateCount = (int)boostIntensity - 1; // Subtract 1 because original hand already exists
        float spacing = 2.0f; // Spacing between duplicated hands

        for (int i = 0; i < duplicateCount; i++)
        {
            try
            {
                // Calculate position offset for this duplicate
                Vector3 offset = new Vector3((i + 1) * spacing, 0, 0);
                Vector3 duplicatePosition = handGameObject.transform.position + offset;

                // Instantiate the duplicate
                GameObject duplicate = Instantiate(handGameObject, duplicatePosition, handGameObject.transform.rotation, handGameObject.transform.parent);
                duplicate.name = handGameObject.name + "_Boost_" + (i + 1);

                // Disable animation components to prevent conflicts
                HandPoseAnimator handPoseAnimator = duplicate.GetComponent<HandPoseAnimator>();
                if (handPoseAnimator != null)
                {
                    handPoseAnimator.enabled = false;
                    UnityEngine.Debug.Log($"Disabled HandPoseAnimator on {duplicate.name}");
                }

                PoseAnimator poseAnimator = duplicate.GetComponent<PoseAnimator>();
                if (poseAnimator != null)
                {
                    poseAnimator.enabled = false;
                    UnityEngine.Debug.Log($"Disabled PoseAnimator on {duplicate.name}");
                }

                // Disable any other animation-related components
                Animator animator = duplicate.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                    UnityEngine.Debug.Log($"Disabled Animator on {duplicate.name}");
                }

                // In edit mode, mark the duplicate as not editable to prevent further issues
                if (!Application.isPlaying)
                {
                    duplicate.hideFlags = HideFlags.DontSaveInEditor;
                }

                // Add to tracking list
                duplicatedHands.Add(duplicate);

                UnityEngine.Debug.Log($"Duplicated hand: {duplicate.name} at position {duplicatePosition}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to duplicate hand {i + 1}: {e.Message}");
            }
        }

        UnityEngine.Debug.Log($"Boost intensity {boostIntensity}: Created {duplicateCount} hand duplicates");
    }

    // Clean up duplicated hand gameobjects
    void CleanupDuplicatedHands()
    {
        foreach (GameObject duplicate in duplicatedHands)
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
        duplicatedHands.Clear();
    }

    // Method to change boost intensity at runtime
    public void SetBoostIntensity(BoostIntensity newIntensity)
    {
        if (boostIntensity != newIntensity)
        {
            boostIntensity = newIntensity;
            DuplicateHands(); // Recreate duplicates with new intensity
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