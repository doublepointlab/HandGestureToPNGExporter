using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Collections;

public class FrameRecorder : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // The camera to capture frames from
    [SerializeField] private FadeInOut fadeInOut; // Reference to FadeInOut script
    [SerializeField] private MoreSettings moreSettings = new MoreSettings();

    [SerializeField] private bool exportAsMP4 = false; // Export as MP4
    [SerializeField] private bool exportAsPNGSequence = true; // Export as PNG Sequence
    [SerializeField] private bool exportAsJPGSequence = false; // Export as JPG Sequence
    [SerializeField] private bool exportAsGIF = false; // Export as GIF
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

    void Awake()
    {
        backgroundColor = moreSettings._backgroundColor;
        outputWidth = moreSettings._outputWidth;
        outputHeight = moreSettings._outputHeight;
        outputScale = moreSettings._outputScale;
        frameRate = moreSettings._frameRate;
        gifFrameRate = moreSettings._gifFrameRate;
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
            if (isFadingOut && frameCount == (endFrame - (int)((fadeDuration+1) * frameRate)))
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
        if (exportAsPNGSequence || exportAsMP4)
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
            OpenOutputFolder(); // Open the output folder
        }
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
        if (exportAsMP4 && !exportAsPNGSequence)
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