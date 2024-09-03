using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEditor.Formats.Fbx.Exporter; // Include the ModelExporter

public class HandPoseAnimator : MonoBehaviour
{
    public GameObject[] handModels; // Array of hand models to interpolate between.
    public float[] animationDurations; // Array of animation durations for each hand model.
    public bool loopAnimation = true; // Should the animation loop?

    private GameObject instantiatedObject; // To keep track of the instantiated object.
    private bool isAnimating = false; // To check if animation is in progress.
    private int currentModelIndex = 0; // To keep track of the current hand model.

    void Start()
    {
        PlayAnimation(); // Start the animation when the game starts
    }

    [ContextMenu("Play Animation")]
    public void PlayAnimation()
    {
        if (!Application.isPlaying) // Ensure this only runs in Play mode
        {
            Debug.LogWarning("Play Animation can only be triggered in Play mode.");
            return;
        }

        if (isAnimating) // Prevent starting a new animation if one is already in progress
        {
            Debug.LogWarning("Animation is already playing. Please wait for it to finish.");
            return;
        }

        if (handModels == null || handModels.Length < 2)
        {
            Debug.LogError("Please assign at least two hand models to the array.");
            return;
        }

        if (animationDurations == null || animationDurations.Length != handModels.Length)
        {
            Debug.LogError("Please assign an animation duration for each hand model.");
            return;
        }

        // Stop any ongoing animation
        StopAnimation();

        // Reset animation state
        currentModelIndex = 0; // Reset to the first model
        isAnimating = true; // Set animating state to true

        // Hide original hand models
        HideOriginalHandModels();

        // Start a new animation sequence
        InstantiateAndAnimateFirstObject();
    }

    public void StopAnimation()
    {
        if (isAnimating)
        {
            CancelInvoke(nameof(AnimateCurrentModel)); // Cancel any pending invokes
            if (instantiatedObject != null)
            {
                DestroyImmediate(instantiatedObject);
            }
            isAnimating = false; // Reset the animation state
        }
    }

    private void InstantiateAndAnimateFirstObject()
    {
        // Instantiate the first hand model
        instantiatedObject = Instantiate(handModels[currentModelIndex], transform.position, transform.rotation, transform);
        instantiatedObject.name = handModels[currentModelIndex].name + "_Clone";
        instantiatedObject.SetActive(true);

        // Start animating the first model
        AnimateCurrentModel();
    }

    private void AnimateCurrentModel()
    {
        if (currentModelIndex >= handModels.Length)
        {
            if (loopAnimation)
            {
                currentModelIndex = 0; // Loop back to the first model
            }
            else
            {
                StopAnimation(); // Stop if not looping
                return;
            }
        }

        // Get the next model index
        int nextModelIndex = currentModelIndex + 1;

        // If there's a next model, create and apply the animation
        if (nextModelIndex < handModels.Length)
        {
            if (instantiatedObject != null) // Check if the object is not null
            {
                CreateAndApplyAnimation(instantiatedObject, handModels[currentModelIndex], handModels[nextModelIndex]);
                currentModelIndex++; // Move to the next model
            }
        }

        // Schedule the next animation
        if (instantiatedObject != null) // Check if the object is not null
        {
            Invoke(nameof(AnimateCurrentModel), animationDurations[currentModelIndex]);
        }
    }

    private void CreateAndApplyAnimation(GameObject target, GameObject fromModel, GameObject toModel)
    {
        if (target == null) return; // Check if the target is null

        // Create an AnimationClip
        AnimationClip clip = new AnimationClip();
        clip.legacy = false;

        // Get all relevant child transforms of the fromModel and toModel
        List<Transform> fromTransforms = GetRelevantChildTransforms(fromModel.transform);
        List<Transform> toTransforms = GetRelevantChildTransforms(toModel.transform);

        // Ensure the target has the same hierarchy structure
        List<Transform> targetTransforms = GetRelevantChildTransforms(target.transform);

        if (fromTransforms.Count != toTransforms.Count || fromTransforms.Count != targetTransforms.Count)
        {
            Debug.LogError("Transform hierarchy mismatch between models.");
            return;
        }

        // Create keyframes for each relevant child transform
        for (int i = 0; i < fromTransforms.Count; i++)
        {
            Transform fromTransform = fromTransforms[i];
            Transform toTransform = toTransforms[i];
            Transform targetTransform = targetTransforms[i];

            // Position keyframes
            Keyframe[] positionKeys = new Keyframe[2];
            positionKeys[0] = new Keyframe(0, fromTransform.localPosition.x);
            positionKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localPosition.x);
            AnimationCurve curveX = new AnimationCurve(positionKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localPosition.x", curveX);

            positionKeys[0] = new Keyframe(0, fromTransform.localPosition.y);
            positionKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localPosition.y);
            AnimationCurve curveY = new AnimationCurve(positionKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localPosition.y", curveY);

            positionKeys[0] = new Keyframe(0, fromTransform.localPosition.z);
            positionKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localPosition.z);
            AnimationCurve curveZ = new AnimationCurve(positionKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localPosition.z", curveZ);

            // Rotation keyframes
            Keyframe[] rotationKeys = new Keyframe[2];
            rotationKeys[0] = new Keyframe(0, fromTransform.localRotation.eulerAngles.x);
            rotationKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localRotation.eulerAngles.x);
            AnimationCurve curveRotX = new AnimationCurve(rotationKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localRotation.eulerAngles.x", curveRotX);

            rotationKeys[0] = new Keyframe(0, fromTransform.localRotation.eulerAngles.y);
            rotationKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localRotation.eulerAngles.y);
            AnimationCurve curveRotY = new AnimationCurve(rotationKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localRotation.eulerAngles.y", curveRotY);

            rotationKeys[0] = new Keyframe(0, fromTransform.localRotation.eulerAngles.z);
            rotationKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localRotation.eulerAngles.z);
            AnimationCurve curveRotZ = new AnimationCurve(rotationKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localRotation.eulerAngles.z", curveRotZ);

            // Scale keyframes
            Keyframe[] scaleKeys = new Keyframe[2];
            scaleKeys[0] = new Keyframe(0, fromTransform.localScale.x);
            scaleKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localScale.x);
            AnimationCurve curveScaleX = new AnimationCurve(scaleKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localScale.x", curveScaleX);

            scaleKeys[0] = new Keyframe(0, fromTransform.localScale.y);
            scaleKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localScale.y);
            AnimationCurve curveScaleY = new AnimationCurve(scaleKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localScale.y", curveScaleY);

            scaleKeys[0] = new Keyframe(0, fromTransform.localScale.z);
            scaleKeys[1] = new Keyframe(animationDurations[currentModelIndex], toTransform.localScale.z);
            AnimationCurve curveScaleZ = new AnimationCurve(scaleKeys);
            clip.SetCurve(AnimationUtility.CalculateTransformPath(targetTransform, target.transform), typeof(Transform), "localScale.z", curveScaleZ);
        }

        // Create an AnimatorController
        AnimatorController animatorController = AnimatorController.CreateAnimatorControllerAtPath("Assets/AnimatorController.controller");
        animatorController.AddMotion(clip);

        // Add Animator component to the target object if it doesn't already have one
        Animator animator = target.GetComponent<Animator>();
        if (animator == null)
        {
            animator = target.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;

        // Save the AnimationClip as an asset
        AssetDatabase.CreateAsset(clip, "Assets/AnimationClip.anim");
        AssetDatabase.SaveAssets();
    }

    private void HideOriginalHandModels()
    {
        foreach (GameObject handModel in handModels)
        {
            handModel.SetActive(false);
        }
    }

    private List<Transform> GetRelevantChildTransforms(Transform parent)
    {
        List<Transform> transforms = new List<Transform>();
        foreach (Transform child in parent)
        {
            transforms.Add(child);
            transforms.AddRange(GetRelevantChildTransforms(child));
        }
        return transforms;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // Check for spacebar press
        {
            PlayAnimation(); // Trigger the animation
        }

        if (isAnimating)
        {
            LeanTween.update();
        }
    }

    // Method to export the instantiated object as FBX
    public void ExportFBX()
    {
        List<GameObject> objectsToExport = new List<GameObject>();

        if (instantiatedObject != null)
        {
            objectsToExport.Add(instantiatedObject);
        }
        else
        {
            foreach (GameObject handModel in handModels)
            {
                if (handModel.activeSelf)
                {
                    objectsToExport.Add(handModel);
                }
            }

            if (objectsToExport.Count == 0)
            {
                Debug.LogError("No instantiated object or visible hand models to export.");
                return;
            }
        }

        string path = EditorUtility.SaveFilePanel("Export FBX", "", "ExportedObjects.fbx", "fbx");
        if (string.IsNullOrEmpty(path))
            return;

        // Use the FBX Exporter to export the objects
        foreach (GameObject obj in objectsToExport)
        {
            ModelExporter.ExportObject(path, obj);
        }
        Debug.Log("Exported FBX to: " + path);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HandPoseAnimator))]
public class HandPoseAnimatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HandPoseAnimator handPoseAnimator = (HandPoseAnimator)target;
        if (GUILayout.Button("Play Animation"))
        {
            if (Application.isPlaying) // Ensure this only runs in Play mode
            {
                handPoseAnimator.PlayAnimation();
            }
            else
            {
                Debug.LogWarning("Play Animation can only be triggered in Play mode.");
            }
        }

        if (GUILayout.Button("Stop Animation"))
        {
            if (Application.isPlaying) // Ensure this only runs in Play mode
            {
                handPoseAnimator.StopAnimation();
            }
            else
            {
                Debug.LogWarning("Stop Animation can only be triggered in Play mode.");
            }
        }

        if (GUILayout.Button("Export FBX"))
        {
            handPoseAnimator.ExportFBX();
        }
    }
}
#endif