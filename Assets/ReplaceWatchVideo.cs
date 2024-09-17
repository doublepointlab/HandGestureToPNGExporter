using UnityEngine;
using UnityEngine.Video;

[ExecuteInEditMode]
public class ReplaceWatchVideo : MonoBehaviour
{
    [SerializeField] private PoseAnimator poseAnimator;
    [SerializeField] VideoClip videoClip;
    private bool isVideoPlaying;
    private int previousAnimationCount = -1;
    private VideoPlayer[] allVideoPlayers;

    private void Awake()
    {
        ReplaceAndPlayVideo();
    }

    private void OnEnable()
    {
        ReplaceAndPlayVideo();
    }

    private void Start()
    {
        if (allVideoPlayers != null)
        {
            foreach (VideoPlayer player in allVideoPlayers)
            {
                player.Play();
            }
        }
    }

    private void Update()
    {
        if (poseAnimator != null && allVideoPlayers != null)
        {
            if (poseAnimator.animationCount != previousAnimationCount)
            {
                foreach (VideoPlayer player in allVideoPlayers)
                {
                    player.time = 0; // Restart the video
                    player.Play();
                }
                previousAnimationCount = poseAnimator.animationCount;
            }
        }
    }

    private void ReplaceAndPlayVideo()
    {
        if (videoClip == null) return;

        allVideoPlayers = FindObjectsOfType<VideoPlayer>();
        foreach (VideoPlayer player in allVideoPlayers)
        {
            if (player.clip != videoClip)
            {
                player.clip = videoClip;
                player.Prepare();
                isVideoPlaying = false;
            }
            else if (!isVideoPlaying)
            {
                player.Play();
                isVideoPlaying = true;
            }
        }
    }

    private void OnDisable()
    {
        if (allVideoPlayers != null)
        {
            foreach (VideoPlayer player in allVideoPlayers)
            {
                if (player != null && player.isPlaying)
                {
                    player.Pause();
                    isVideoPlaying = false;
                }
            }
        }
    }
}