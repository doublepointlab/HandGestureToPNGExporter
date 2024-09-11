using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
[ExecuteInEditMode]
public class AdvancedVideoSettings : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    private static bool isAnyVideoPlaying = false;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    void Update()
    {
        if (videoPlayer != null && !videoPlayer.isPlaying && !isAnyVideoPlaying)
        {
            videoPlayer.Play();
            isAnyVideoPlaying = true;
        }
    }

    void OnDisable()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            isAnyVideoPlaying = false;
        }
    }
}
