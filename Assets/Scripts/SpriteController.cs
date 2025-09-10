using UnityEngine;
using UnityEngine.UI;
using System;

public class SpriteController : MonoBehaviour
{
    [SerializeField] private Image sourceImage;
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private PoseAnimator poseAnimator;
    [SerializeField] private int changeSpriteAfterPoseIndex = 1;
    
    private bool hasTriggeredForPoseIndex = false;
    private Sprite originalSourceSprite;
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
        // Validate source image component
        if (sourceImage == null)
        {
            Debug.LogWarning("SpriteController: No source Image component assigned. Please assign an Image component.");
            return;
        }
        
        // Validate target sprite
        if (targetSprite == null)
        {
            Debug.LogWarning("SpriteController: No target sprite assigned. Please assign a target sprite.");
            return;
        }
        
        // Store the original source sprite for resetting
        originalSourceSprite = sourceImage.sprite;
        
        Debug.Log($"SpriteController initialized. Will change sprite when pose index reaches {changeSpriteAfterPoseIndex}");
    }
    
    /// <summary>
    /// Check current pose index and trigger sprite change if needed
    /// </summary>
    private void CheckPoseIndex()
    {
        if (poseAnimator == null) return;
        
        int currentPoseIndex = poseAnimator.CurrentPoseIndex;
        
        // Check if we've passed the threshold and haven't triggered yet
        if (currentPoseIndex >= changeSpriteAfterPoseIndex && !hasTriggeredForPoseIndex)
        {
            hasTriggeredForPoseIndex = true;
            ChangeSprite();
            Debug.Log($"Sprite changed at pose index {currentPoseIndex} (threshold: {changeSpriteAfterPoseIndex})");
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
    private void CheckForPoseCycleCompletion(int currentPoseIndex)
    {
        // If we've triggered the sprite change and pose index goes back to 0 (cycle completion)
        if (hasTriggeredForPoseIndex && currentPoseIndex == 0 && lastPoseIndex > 0)
        {
            ResetToSourceSprite();
            hasTriggeredForPoseIndex = false; // Reset trigger state for next cycle
            Debug.Log("Pose cycle completed - sprite reset to source");
        }
    }
    
    /// <summary>
    /// Change the sprite to target sprite
    /// </summary>
    public void ChangeSprite()
    {
        if (sourceImage != null && targetSprite != null)
        {
            // Change sprite to target sprite
            sourceImage.sprite = targetSprite;
            OnSpriteChanged?.Invoke();
            Debug.Log($"Sprite changed to target sprite at pose index {changeSpriteAfterPoseIndex}");
        }
    }
    
    /// <summary>
    /// Reset sprite to original source sprite
    /// </summary>
    public void ResetToSourceSprite()
    {
        if (sourceImage != null && originalSourceSprite != null)
        {
            sourceImage.sprite = originalSourceSprite;
            OnSpriteChanged?.Invoke();
            Debug.Log("Sprite reset to original source sprite");
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
        Debug.Log($"Change sprite after pose index set to {poseIndex}");
    }
    
    /// <summary>
    /// Manually trigger sprite change (for testing or external triggers)
    /// </summary>
    [ContextMenu("Change Sprite")]
    public void ManualSpriteChange()
    {
        ChangeSprite();
    }
    
    /// <summary>
    /// Manually reset sprite to source (for testing)
    /// </summary>
    [ContextMenu("Reset to Source Sprite")]
    public void ManualResetToSource()
    {
        ResetToSourceSprite();
    }
}
