using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LookAtHand : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private bool applyRotation = true; // Added bool to control rotation

    private void Update()
    {
        if (target != null && !applyRotation) // Check if rotation should be applied
        {
            Vector3 originalRotation = transform.localEulerAngles;
            transform.LookAt(target);
            transform.localEulerAngles = new Vector3(originalRotation.x, transform.localEulerAngles.y, originalRotation.z);
        }
        else // If rotation should not be applied
        {
            transform.LookAt(target);
        }
    }
}
