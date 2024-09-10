using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class FrameRecorder : MonoBehaviour
{
    [SerializeField] private Camera targetCamera; // The camera to capture frames from
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0); // Background color
    [SerializeField] private string outputFolder = "Frames"; // The folder to save the frames
    [SerializeField] private int outputWidth = 1920; // The width of the output image
    [SerializeField] private int outputHeight = 1080; // The height of the output image
    public int frameRate = 30; // The frame rate for capturing frames
    public int gifFrameRate = 10; // The frame rate for capturing frames
    [SerializeField] private bool exportAsMP4 = false; // Export as MP4
    [SerializeField] private bool exportAsPNGSequence = true; // Export as PNG Sequence
    [SerializeField] private bool exportAsJPGSequence = false; // Export as JPG Sequence
    [SerializeField] private bool exportAsGIF = false; // Export as GIF
    [SerializeField] private int[] excludedFrames = { 24, 53, 88 }; // Frames to exclude from export

    private int startFrame = 0; // The frame to start recording
    [HideInInspector] public int endFrame = 100; // The frame to stop recording
    private int frameCount = 0;
    private int actualFrameCount = 0; // Counter for actual saved frames
     

    void Start()
    {
        if (gifFrameRate > frameRate)
        {
            UnityEngine.Debug.LogError("GIF frame rate cannot be higher than the main frame rate.");
            return;
        }        
        CreateOutputFolder();
        ConfigureCamera();
        StartRecording();
        
    }

    void LateUpdate()
    {
        if (frameCount >= startFrame && frameCount <= endFrame && System.Array.IndexOf(excludedFrames, frameCount) == -1)
        {
            CaptureFrame();
        }
        frameCount++;
    }

    void CaptureFrame()
    {
        RenderTexture renderTexture = new RenderTexture(outputWidth, outputHeight, 24, RenderTextureFormat.ARGB32);
        targetCamera.targetTexture = renderTexture;
        targetCamera.Render();

        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        byte[] bytes = null;
        string filePath = string.Empty;
        if (exportAsJPGSequence || exportAsGIF)
        {
            bytes = texture.EncodeToJPG();
            filePath = Path.Combine(outputFolder, "JPGSequence", $"{outputFolder}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        }        
        if (exportAsPNGSequence || exportAsMP4)
        {
            bytes = texture.EncodeToPNG();
            filePath = Path.Combine(outputFolder, "PNGSequence", $"{outputFolder}_frame_{actualFrameCount:D}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        } 
        if (exportAsJPGSequence && exportAsPNGSequence)
        {
            bytes = texture.EncodeToJPG();
            filePath = Path.Combine(outputFolder, "JPGSequence", $"{outputFolder}_frame_{actualFrameCount:D}.jpg");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
            bytes = texture.EncodeToPNG();
            filePath = Path.Combine(outputFolder, "PNGSequence", $"{outputFolder}_frame_{actualFrameCount:D}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllBytes(filePath, bytes);
        } 

        targetCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(texture);

        actualFrameCount++;
    }

    public void StartRecording()
    {
        frameCount = 0;
        actualFrameCount = 0;
        Time.captureFramerate = frameRate;
        UnityEngine.Debug.Log("Recording started.");
        EditorApplication.EnterPlaymode();
    }

    void OnApplicationQuit()
    {
        HideFrameFiles();
        if (exportAsMP4)
        {            
            ExportToMP4();            
        }
        if (exportAsGIF)
        {
            ExportToGIF();
        }
        UnhideFrameFiles();
        OpenOutputFolder();
    }

    void OpenOutputFolder()
    {
        #if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", outputFolder);
        #elif UNITY_EDITOR_OSX
            Process.Start("open", outputFolder);
        #endif
    }

    void ExportToMP4()
    {
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine(outputFolder, "PNGSequence", $"{outputFolder}_frame_%d.png");
        string outputFilePath = Path.Combine(outputFolder, $"_{outputFolder}.mp4");

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
            DeleteFiles("PNGSequence", "*.png");
        }
    }

    void ExportToGIF()
    {
        string ffmpegPath = "/usr/local/bin/ffmpeg";
        string inputPattern = Path.Combine(outputFolder, "JPGSequence", $"{outputFolder}_frame_%d.jpg");
        string outputFilePath = Path.Combine(outputFolder, $"_{outputFolder}.gif");

        ProcessStartInfo processStartInfo = new ProcessStartInfo(ffmpegPath)
        {
            //Arguments = $"-i \"{inputPattern}\" -vf \"fps={gifFrameRate},setpts=(1/({gifFrameRate}/10))*PTS\" -gifflags +transdiff \"{outputFilePath}\"",
            Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -vf \"fps={gifFrameRate}\" -gifflags +transdiff \"{outputFilePath}\"",
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
            DeleteFiles("JPGSequence", "*.jpg");
        }
    }

    void CreateOutputFolder()
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }
        else
        {
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
    }

    void ConfigureCamera()
    {
        targetCamera.clearFlags = CameraClearFlags.SolidColor;
        targetCamera.backgroundColor = backgroundColor;
    }

    void DeleteFiles(string subFolder, string pattern)
    {
        string[] files = Directory.GetFiles(Path.Combine(outputFolder, subFolder), pattern);
        foreach (string file in files)
        {
            File.Delete(file);
        }
    }

    void HideFrameFiles()
    {
        string[] files = Directory.GetFiles(Path.Combine(outputFolder, "PNGSequence"), "*.png");
        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Hidden);
        }
    }

    void UnhideFrameFiles()
    {
        string[] files = Directory.GetFiles(Path.Combine(outputFolder, "PNGSequence"), "*.png");
        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }
    }
}