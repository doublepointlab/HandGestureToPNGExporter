using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PoseAnimator : MonoBehaviour
{
    [SerializeField] private Transform[] handPoses;
    
    [SerializeField, Range(0, 0.7f)] public float[] durations; // Array of durations for each transition
    
    private int currentPoseIndex = 0;
    private int targetPoseIndex = 1;
    
    private Transform[] currentPoseHandJoints;
    private Transform[] targetPoseHandJoints;
    private Transform[] visiblePoseHandJoints;
    private float elapsedTime = 0;

    private float totalFrames30fps = 0;
    private float totalFrames60fps = 0;
    private float totalTime = 0;
    
    private void Start()
    {
        UpdateCurrentPose(0);
        UpdateTargetPose(1);
        visiblePoseHandJoints = transform.GetComponentsInChildren<Transform>();       
        CalculateTotalFramesAndTime();
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
    }

    private void CalculateTotalFramesAndTime()
    {
        totalFrames30fps = 0;
        totalFrames60fps = 0;
        totalTime = 0;
        
        foreach (var duration in durations)
        {
            totalFrames30fps += Mathf.RoundToInt(duration * 30); // Convert duration to frame count at 30fps
            totalFrames60fps += Mathf.RoundToInt(duration * 60); // Convert duration to frame count at 60fps
            totalTime += duration;
        }
    }

    private void OnValidate()
    {
        CalculateTotalFramesAndTime();       
        Debug.Log("Total frame count of the animation at 30fps: " + totalFrames30fps + ", at 60fps: " + totalFrames60fps + ", Total time of the animation in seconds: " + totalTime);
    }
}