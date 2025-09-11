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
        if (durations != null && durations.Length > 0)
        {
            // Ensure startElement and endElement are within bounds
            int safeStartElement = Mathf.Clamp(startElement, 0, durations.Length - 1);
            int safeEndElement = Mathf.Clamp(endElement, 0, durations.Length - 1);
            
            for (int i = safeStartElement; i <= safeEndElement; i++)
            {
                calculatedDuration += durations[i];
            }
        }
        else
        {
            Debug.LogError("PoseAnimator or durations array is null or empty.");
        }
    }
}
