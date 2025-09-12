using UnityEngine;
using UnityEngine.UI;
using System;

public class SpriteController : MonoBehaviour
{
    [SerializeField] private Image referenceImage; // Image to apply target sprite to
    [SerializeField] private Sprite sourceSprite; // Idle state sprite
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private PoseAnimator poseAnimator;
    [SerializeField] private GlobalFrameRecorder globalFrameRecorder; // Reference to GlobalFrameRecorder
    [SerializeField] private int changeSpriteAfterPoseIndex = 1;
    
    [Header("Bounce Animation (Uses GlobalFrameRecorder settings)")]
    
    private bool hasTriggeredForPoseIndex = false;
    private Sprite originalReferenceSprite;
    private int lastPoseIndex = -1;
    
    // Events
    public static event Action OnSpriteChanged;
    
    private void Start()
    {
        
        InitializeSprite();
    }
    
    private void Update()
    {
        CheckPoseIndex();
    }
    
    /// <summary>
    /// Initialize the sprite system
    /// </summary>
    public void InitializeSprite()
    {
        // Validate source sprite (idle state)
        if (sourceSprite == null)
        {
            //Debug.LogWarning("SpriteController: No source sprite assigned. Please assign a source sprite for idle state.");
            return;
        }
        
        // Validate reference image component
        if (referenceImage == null)
        {
            //Debug.LogWarning("SpriteController: No reference Image component assigned. Please assign a reference Image component.");
            return;
        }
        
        // Target sprite is optional - if not set, only bounce animation will occur
        
        // Store the original reference sprite for resetting
        originalReferenceSprite = referenceImage.sprite;
        
        // Set initial state: apply source sprite to reference image (idle)
        referenceImage.sprite = sourceSprite;
        
        //Debug.Log($"SpriteController initialized. Will change sprite when pose index reaches {changeSpriteAfterPoseIndex}");
    }
    
    /// <summary>
    /// Check current pose index and trigger sprite change if needed
    /// </summary>
    private void CheckPoseIndex()
    {
        if (poseAnimator == null) return;
        
        int currentPoseIndex = poseAnimator.CurrentPoseIndex;
        
        // Debug logging to help diagnose issues
        if (currentPoseIndex != lastPoseIndex)
        {
            //Debug.Log($"Pose index changed: {lastPoseIndex} -> {currentPoseIndex} (threshold: {changeSpriteAfterPoseIndex}, triggered: {hasTriggeredForPoseIndex})");
        }
        
        // Check if we've passed the threshold and haven't triggered yet
        if (currentPoseIndex >= changeSpriteAfterPoseIndex && !hasTriggeredForPoseIndex)
        {
            hasTriggeredForPoseIndex = true;
            TriggerSpriteChange();
            //Debug.Log($"Sprite triggered at pose index {currentPoseIndex} (threshold: {changeSpriteAfterPoseIndex})");
        }
        
        // Check for pose cycle completion (reset to source sprite)
        CheckForPoseCycleCompletion(currentPoseIndex);
        
        // Update last pose index for next frame
        lastPoseIndex = currentPoseIndex;
    }
    
    /// <summary>
    /// Check if pose animator has completed a cycle and reset sprite to source
    /// </summary>
    /// <param name="currentPoseIndex">Current pose index</param>
    private int framesSinceCycleComplete = 0;
    private bool waitingToResetSprite = false;

    private void CheckForPoseCycleCompletion(int currentPoseIndex)
    {
        // If we've triggered the sprite change and pose index goes back to 0 (cycle completion)
        if (hasTriggeredForPoseIndex && currentPoseIndex == 0 && lastPoseIndex > 0)
        {
            waitingToResetSprite = true;
            framesSinceCycleComplete = 0;
            //Debug.Log("Pose cycle completed - waiting to reset sprite after 5 frames");
        }

        if (waitingToResetSprite)
        {
            framesSinceCycleComplete++;
            if (framesSinceCycleComplete >= 5)
            {
                ResetToSourceSprite();
                hasTriggeredForPoseIndex = false; // Reset trigger state for next cycle
                waitingToResetSprite = false;
                framesSinceCycleComplete = 0;
                //Debug.Log("Sprite reset to source after 5 frames");
            }
        }
    }
    
    /// <summary>
    /// Trigger sprite change and bounce animation
    /// </summary>
    public void TriggerSpriteChange()
    {
        // Change sprite if target sprite is available
        if (targetSprite != null)
        {
            ChangeSprite();
        }
        
        // Trigger bounce animation directly on the reference image
        TriggerBounceAnimation();
    }
    
    /// <summary>
    /// Change the sprite to target sprite (apply to reference image)
    /// </summary>
    public void ChangeSprite()
    {
        if (referenceImage == null || targetSprite == null) return;
        
        // Apply target sprite to reference image
        referenceImage.sprite = targetSprite;
        
        OnSpriteChanged?.Invoke();
        //Debug.Log($"Sprite changed to target sprite: {targetSprite.name}");
    }
    
    /// <summary>
    /// Reset to source sprite (idle state)
    /// </summary>
    public void ResetToSourceSprite()
    {
        if (referenceImage != null && sourceSprite != null)
        {
            // Apply source sprite to reference image (idle state)
            referenceImage.sprite = sourceSprite;
            
            OnSpriteChanged?.Invoke();
            //Debug.Log("Reset to source sprite (idle state)");
        }
    }
    
    /// <summary>
    /// Set the pose index after which to change sprite
    /// </summary>
    /// <param name="poseIndex">New pose index threshold</param>
    public void SetChangeSpriteAfterPoseIndex(int poseIndex)
    {
        changeSpriteAfterPoseIndex = poseIndex;
        hasTriggeredForPoseIndex = false; // Reset trigger state
        //Debug.Log($"Change sprite after pose index set to {poseIndex}");
    }
    
    /// <summary>
    /// Manually trigger sprite change (for testing or external triggers)
    /// </summary>
    [ContextMenu("Change Sprite")]
    public void ManualSpriteChange()
    {
        TriggerSpriteChange();
    }
    
    /// <summary>
    /// Manually trigger bounce animation (for testing)
    /// </summary>
    [ContextMenu("Trigger Bounce")]
    public void ManualBounceAnimation()
    {
        TriggerBounceAnimation();
    }    
    /// <summary>
    /// Manually reset sprite to source (for testing)
    /// </summary>
    [ContextMenu("Reset to Source Sprite")]
    public void ManualResetToSource()
    {
        ResetToSourceSprite();
    }
    
    /// <summary>
    /// Trigger bounce animation on the reference image using GlobalFrameRecorder settings
    /// </summary>
    private void TriggerBounceAnimation()
    {
        if (referenceImage == null || globalFrameRecorder == null) return;
        
        // Get bounce settings from GlobalFrameRecorder
        var (enabled, duration, intensity) = globalFrameRecorder.GetBounceSettings();
        
        if (!enabled) return;
        
        // Store original scale
        Vector3 originalScale = referenceImage.transform.localScale;
        
        // Create bounce animation using LeanTween
        LeanTween.scale(referenceImage.gameObject, originalScale * intensity, duration * 0.5f)
            .setEaseOutQuad()
            .setOnComplete(() => {
                LeanTween.scale(referenceImage.gameObject, originalScale, duration * 0.5f)
                    .setEaseInQuad();
            });
        
        UnityEngine.Debug.Log($"Bounce animation triggered on {referenceImage.name} (intensity: {intensity}, duration: {duration})");
    }
    
    
}
