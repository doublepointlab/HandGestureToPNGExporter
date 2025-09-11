using UnityEngine;
using UnityEditor;

public class IconImageController : MonoBehaviour
{
    /// <summary>
    /// Force update all SpriteController reference images in the scene
    /// </summary>
    [MenuItem("Tools/Force Update All SpriteController References")]
    public static void ForceUpdateAllSpriteControllerReferences()
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
    
    /// <summary>
    /// Force update selected GameObject's SpriteController reference image
    /// </summary>
    [MenuItem("Tools/Force Update Selected SpriteController Reference")]
    public static void ForceUpdateSelectedSpriteControllerReference()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("IconImageController: No GameObject selected.");
            return;
        }
        
        SpriteController spriteController = Selection.activeGameObject.GetComponent<SpriteController>();
        if (spriteController == null)
        {
            Debug.LogWarning($"IconImageController: Selected GameObject {Selection.activeGameObject.name} does not have a SpriteController component.");
            return;
        }
        
        // Trigger OnValidate to update reference image
        spriteController.SendMessage("OnValidate", null, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"IconImageController: Force updated reference image for {Selection.activeGameObject.name}.");
    }
    
    /// <summary>
    /// Auto-assign and update reference image for this specific SpriteController
    /// </summary>
    [ContextMenu("Update Reference Image")]
    public void UpdateReferenceImage()
    {
        SpriteController spriteController = GetComponent<SpriteController>();
        if (spriteController != null)
        {
            spriteController.SendMessage("OnValidate", null, SendMessageOptions.DontRequireReceiver);
            Debug.Log($"IconImageController: Updated reference image for {gameObject.name}.");
        }
        else
        {
            Debug.LogWarning($"IconImageController: {gameObject.name} does not have a SpriteController component.");
        }
    }
}
