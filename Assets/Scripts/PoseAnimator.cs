using UnityEngine;
using UnityEditor;

public class PoseAnimator : MonoBehaviour
{
    [SerializeField] private Transform[] handPoses;
    [Range(0, 0.25f)] public float[] durations; // Array of durations for each transition
    
    private int currentPoseIndex = 0;
    private int targetPoseIndex = 1;
    
    private Transform[] currentPoseHandJoints;
    private Transform[] targetPoseHandJoints;
    private Transform[] visiblePoseHandJoints;
    private float elapsedTime = 0;
    private float accumulatedTime = 0;

    private float totalFrames = 0;
    private float totalTime = 0;

    [SerializeField] private FrameRecorder frameRecorder;
    private int frameRate = 0;
    [HideInInspector] public int animationCount = 0;
    
    private void Start()
    {
        if (handPoses.Length != durations.Length)
        {
            Debug.LogError($"The amount of handPoses and durations must be identical in: {transform.parent.gameObject.name}");
        }
        
        UpdateCurrentPose(0);
        UpdateTargetPose(1);
        visiblePoseHandJoints = transform.GetComponentsInChildren<Transform>();       
    }

    private void OnValidate()
    {   
        frameRate = frameRecorder.frameRate;        
        totalFrames = 0;       
        totalTime = 0;       
        foreach (var duration in durations)
        {
            totalFrames += Mathf.RoundToInt(duration * frameRate); // Convert duration to frame count at frameRate
            totalTime += duration;
        }
        frameRecorder.endFrame = (int)totalFrames;
    }
    
    private void UpdateCurrentPose(int newPoseIndex)
    {
        if (handPoses != null && newPoseIndex >= 0 && newPoseIndex < handPoses.Length && handPoses[newPoseIndex] != null)
        {
            currentPoseIndex = newPoseIndex;
            currentPoseHandJoints = handPoses[currentPoseIndex].GetComponentsInChildren<Transform>();
        }
    }
    
    private void UpdateTargetPose(int newPoseIndex)
    {
        if (handPoses != null && newPoseIndex >= 0 && newPoseIndex < handPoses.Length && handPoses[newPoseIndex] != null)
        {
            targetPoseIndex = newPoseIndex;
            targetPoseHandJoints = handPoses[targetPoseIndex].GetComponentsInChildren<Transform>();
            visiblePoseHandJoints = transform.GetComponentsInChildren<Transform>(); // Update visiblePoseHandJoints
        }
    }

    private void Update()
    {
        // Safety checks to prevent crashes
        if (durations == null || handPoses == null || currentPoseIndex < 0 || currentPoseIndex >= durations.Length)
        {
            return;
        }

        int totalFramesForCurrentPose = Mathf.RoundToInt(durations[currentPoseIndex] * frameRate);
        int currentFrame = Mathf.RoundToInt(elapsedTime * frameRate);

        elapsedTime += Time.deltaTime;
        accumulatedTime += Time.deltaTime;

        if (currentFrame >= totalFramesForCurrentPose)
        {
            elapsedTime -= durations[currentPoseIndex]; // Reset elapsedTime for the next cycle
            UpdateCurrentPose(targetPoseIndex); // Update current pose to the target pose
            UpdateTargetPose((targetPoseIndex + 1) % handPoses.Length); // Set the next target pose
        }

        float timeToUse = (float)currentFrame / totalFramesForCurrentPose;

        // Skip the first and last frame of interpolation
        if (timeToUse < 0.98f)
        {
            // Safety checks to prevent IndexOutOfRangeException
            if (visiblePoseHandJoints != null && currentPoseHandJoints != null && targetPoseHandJoints != null)
            {
                int minLength = Mathf.Min(visiblePoseHandJoints.Length, currentPoseHandJoints.Length, targetPoseHandJoints.Length);
                
                for (int i = 1; i < minLength; i++)
                {
                    if (visiblePoseHandJoints[i] != null && currentPoseHandJoints[i] != null && targetPoseHandJoints[i] != null)
                    {
                        visiblePoseHandJoints[i].localPosition = Vector3.Lerp(currentPoseHandJoints[i].localPosition, targetPoseHandJoints[i].localPosition, timeToUse);
                        visiblePoseHandJoints[i].localRotation = Quaternion.Slerp(currentPoseHandJoints[i].localRotation, targetPoseHandJoints[i].localRotation, timeToUse);
                    }
                }
            }
        }

        // Exit playmode when the total time has passed
        if (accumulatedTime >= totalTime)
        {
            animationCount++;
            if (frameRecorder.isRecording)
            {
                EditorApplication.ExitPlaymode();
                UnityEngine.Debug.Log("Recording finished.");
            }
            accumulatedTime = 0; // Reset accumulatedTime after completing all durations
        }
    }

}