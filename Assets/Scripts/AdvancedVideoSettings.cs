using UnityEngine;
using UnityEngine.Video;

[ExecuteInEditMode]
public class AdvancedVideoSettings : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private static bool isAnyVideoPlaying = false;
    private VideoClip lastVideoClip;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        lastVideoClip = videoPlayer.clip;
        RefreshVideo();
    }

    void Update()
    {
        if (videoPlayer == null) return;

        RefreshVideo();

        if (!videoPlayer.isPlaying && !isAnyVideoPlaying)
        {
            videoPlayer.frame = 0; // Ensure the video awakes on the first frame
            videoPlayer.Play();
            isAnyVideoPlaying = true;
        }
    }

    private void RefreshVideo()
    {
        if (videoPlayer.clip == lastVideoClip) return;

        // Video has changed, refresh it
        lastVideoClip = videoPlayer.clip;
        videoPlayer.Stop();
        videoPlayer.Prepare();
        videoPlayer.frame = 0; // Start on frame 0
        isAnyVideoPlaying = false;
    }

    void OnDisable()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            isAnyVideoPlaying = false;
        }
    }

    void OnValidate()
    {
        // This will ensure the video is refreshed in the editor when changes are made
        if (videoPlayer == null || videoPlayer.clip == lastVideoClip) return;

        lastVideoClip = videoPlayer.clip;
        videoPlayer.Stop();
        videoPlayer.Prepare();
        isAnyVideoPlaying = false;
    }
}
