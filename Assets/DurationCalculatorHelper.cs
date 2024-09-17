using UnityEngine;

public class DurationCalculatorHelper : MonoBehaviour
{
    [SerializeField] private int startElement;
    [SerializeField] private int endElement;
    [SerializeField] private float calculatedDuration;
    [SerializeField] private PoseAnimator poseAnimator;
    private float[] durations => poseAnimator?.durations;
    private void OnValidate()
    {
        calculatedDuration = 0;
        if (durations != null)
        {
            for (int i = startElement; i <= endElement; i++)
            {
                calculatedDuration += durations[i];
            }
        }
        else
        {
            Debug.LogError("PoseAnimator or durations array is null.");
        }
    }
}
