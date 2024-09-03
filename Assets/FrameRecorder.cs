using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class FrameRecorder : MonoBehaviour
{
    public Camera targetCamera; // The camera to capture frames from
    public int frameRate = 30; // The frame rate for capturing frames
    public string outputFolder = "Frames"; // The folder to save the frames
    public int startFrame = 0; // The frame to start recording
    public int endFrame = 100; // The frame to stop recording
    public int[] excludedFrames = { 24, 53, 88 }; // Frames to exclude from export
    public int outputWidth = 1920; // The width of the output image
    public int outputHeight = 1080; // The height of the output image
    public bool exportAsMP4 = false; // Export as MP4
    public bool exportAsPNGSequence = true; // Export as PNG Sequence

    private int frameCount = 0;
    private int actualFrameCount = 0; // Counter for actual saved frames

    void Start()
    {

        // Create the output folder if it doesn't exist
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        else
        {
            // Empty the output folder if it already exists
            DirectoryInfo di = new DirectoryInfo(outputFolder);
            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        // Ensure the camera's clear flags and background color support transparency
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent background

        StartRecording();
        
    }

    void LateUpdate()
    {

        if (frameCount >= startFrame && frameCount <= endFrame)
        {
            if (System.Array.IndexOf(excludedFrames, frameCount) == -1)
            {
                CaptureFrame();
            }
        }
        frameCount++;
    
    }

    void CaptureFrame()
    {
        // Create a RenderTexture with the specified dimensions
        RenderTexture renderTexture = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32);
        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        // Create a new Texture2D with the same dimensions as the RenderTexture
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        // Read the pixels from the RenderTexture into the Texture2D
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        // Encode the texture to PNG
        byte[] bytes = texture.EncodeToPNG();

        // Save the PNG to the output folder
        string filePath = Path.Combine(outputFolder, $"{outputFolder}_frame_{actualFrameCount:D}.png");
        File.WriteAllBytes(filePath, bytes);

        // Clean up
        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(texture);

        //UnityEngine.Debug.Log($"Captured frame {frameCount}");
        actualFrameCount++; // Increment the actual frame count
    }

    public void StartRecording()
    {
        frameCount = 0;
        actualFrameCount = 0; // Reset the actual frame count
        Time.captureFramerate = frameRate; // Set the capture frame rate
        UnityEngine.Debug.Log("Recording started.");
        EditorApplication.EnterPlaymode();
    }

    void OnApplicationQuit()
    {
        if (exportAsMP4)
        {
            ExportToMP4();
        }
        if (!exportAsPNGSequence)
        {
            DeleteUnnecessaryFiles();
        }
    }

    void ExportToMP4()
    {
        string ffmpegPath = "/usr/local/bin/ffmpeg"; // Ensure ffmpeg is installed and available at this path on macOS
        string inputPattern = Path.Combine(outputFolder, $"{outputFolder}_frame_%d.png");
        string outputFilePath = Path.Combine(outputFolder, $"{outputFolder}.mp4");

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
            UnityEngine.Debug.Log("MP4 export completed.");
        }
    }

    void DeleteUnnecessaryFiles()
    {
        DirectoryInfo di = new DirectoryInfo(outputFolder);
        foreach (FileInfo file in di.GetFiles("*.png"))
        {
            file.Delete();
        }
    }
}