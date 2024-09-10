using UnityEngine;
using UnityEditor;

public class PoseAnimator : MonoBehaviour
{
    [SerializeField] private Transform[] handPoses;
    [SerializeField, Range(0, 0.7f)] private float[] durations; // Array of durations for each transition
    
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
    
    private void Start()
    {
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
        currentPoseIndex = newPoseIndex;
        currentPoseHandJoints = handPoses[currentPoseIndex].GetComponentsInChildren<Transform>();
    }
    
    private void UpdateTargetPose(int newPoseIndex)
    {
        targetPoseIndex = newPoseIndex;
        targetPoseHandJoints = handPoses[targetPoseIndex].GetComponentsInChildren<Transform>();
        visiblePoseHandJoints = transform.GetComponentsInChildren<Transform>(); // Update visiblePoseHandJoints
    }

    private void Update()
    {
        var duration = durations[currentPoseIndex];
        var timeToUse = elapsedTime / duration;

        elapsedTime += Time.deltaTime;
        accumulatedTime += Time.deltaTime;
        if (elapsedTime >= duration)
        {
            elapsedTime -= duration; // Reset elapsedTime for the next cycle
            UpdateCurrentPose(targetPoseIndex); // Update current pose to the target pose
            UpdateTargetPose((targetPoseIndex + 1) % handPoses.Length); // Set the next target pose
        }

        // Skip the first and last frame of interpolation
        if (timeToUse < 0.98f)
        {
            for (int i = 1; i < visiblePoseHandJoints.Length; i++)
            {
                visiblePoseHandJoints[i].localPosition = Vector3.Lerp(currentPoseHandJoints[i].localPosition, targetPoseHandJoints[i].localPosition, timeToUse);
                visiblePoseHandJoints[i].localRotation = Quaternion.Slerp(currentPoseHandJoints[i].localRotation, targetPoseHandJoints[i].localRotation, timeToUse);
            }
        }

        // Exit playmode when the total time has passed
        if (accumulatedTime >= totalTime && frameRecorder.isRecording)
        {
            EditorApplication.ExitPlaymode();
        }
    }
}