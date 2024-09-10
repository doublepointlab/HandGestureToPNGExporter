using UnityEngine;
using UnityEditor;

public class ToggleActiveShortcut : MonoBehaviour
{
    // Add a menu item that toggles the active state of the selected GameObject(s)
    [MenuItem("Tools/Toggle Active State %t")] // %t means Ctrl + T (Cmd + T on Mac)
    static void ToggleActiveState()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj, "Toggle Active State");
            obj.SetActive(!obj.activeSelf);
            EditorUtility.SetDirty(obj);
        }
    }
}