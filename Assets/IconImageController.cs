using UnityEngine;
using UnityEditor;

public class IconImageController : MonoBehaviour
{
    #if UNITY_EDITOR
    private void Awake()
    {
        // Subscribe to GameObject enable events
        SpriteController.OnGameObjectEnabled += OnSpriteControllerGameObjectEnabled;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        SpriteController.OnGameObjectEnabled -= OnSpriteControllerGameObjectEnabled;
    }
    
    /// <summary>
    /// Handle when a SpriteController GameObject is enabled
    /// </summary>
    /// <param name="enabledGameObject">The GameObject that was enabled</param>
    private void OnSpriteControllerGameObjectEnabled(GameObject enabledGameObject)
    {
        if (enabledGameObject == null) return;
        
        SpriteController spriteController = enabledGameObject.GetComponent<SpriteController>();
        if (spriteController != null)
        {
            // Force update the SpriteController's reference image
            spriteController.AutoAssignReferenceImage();
            Debug.Log($"IconImageController: Updated reference image for enabled GameObject: {enabledGameObject.name}");
        }
    }
    
    void OnValidate()
    {
        // Find all SpriteController objects in the scene (including inactive ones)
        SpriteController[] allSpriteControllers = Resources.FindObjectsOfTypeAll<SpriteController>();
        
        int updatedCount = 0;
        
        foreach (SpriteController spriteController in allSpriteControllers)
        {
            if (spriteController != null && spriteController.gameObject.scene.IsValid())
            {
                // Trigger OnValidate to update reference image
                spriteController.SendMessage("OnValidate", null, SendMessageOptions.DontRequireReceiver);
                updatedCount++;
            }
        }
        
        Debug.Log($"IconImageController: Force updated {updatedCount} SpriteController objects.");
    }
    #endif
}
