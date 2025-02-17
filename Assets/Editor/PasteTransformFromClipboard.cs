using UnityEngine;
using UnityEditor;

public class PasteTransformFromClipboard : MonoBehaviour
{
    [MenuItem("Tools/Copy Transform To Clipboard %#&c")] // %#&c means Cmd/Ctrl + Alt/Option + Shift + C
    static void CopyTransform()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No object selected for copying transform.");
            return;
        }

        Transform transform = Selection.activeGameObject.transform;
        string transformData = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}",
            transform.localPosition.x,
            transform.localPosition.y,
            transform.localPosition.z,
            transform.localEulerAngles.x,
            transform.localEulerAngles.y,
            transform.localEulerAngles.z,
            transform.localScale.x,
            transform.localScale.y,
            transform.localScale.z
        );

        GUIUtility.systemCopyBuffer = transformData;
        Debug.Log($"Copied local transform data from {Selection.activeGameObject.name}: {transformData}");
    }

    [MenuItem("Tools/Paste Transform From Clipboard %#&v")] // %#&v means Cmd/Ctrl + Alt/Option + Shift + V
    static void PasteTransform()
    {
        string clipboardText = GUIUtility.systemCopyBuffer;
        if (string.IsNullOrEmpty(clipboardText))
        {
            Debug.LogWarning("Clipboard is empty.");
            return;
        }

        string[] values = clipboardText.Split(',');
        if (values.Length != 9)
        {
            Debug.LogError($"Invalid transform data format. Expected 9 values, got {values.Length}.");
            return;
        }

        try
        {
            Vector3 position = new Vector3(
                float.Parse(values[0]),
                float.Parse(values[1]), 
                float.Parse(values[2])
            );
            Vector3 rotation = new Vector3(
                float.Parse(values[3]),
                float.Parse(values[4]),
                float.Parse(values[5])
            );
            Vector3 scale = new Vector3(
                float.Parse(values[6]),
                float.Parse(values[7]),
                float.Parse(values[8])
            );

            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("No objects selected for pasting transform.");
                return;
            }

            foreach (GameObject obj in Selection.gameObjects)
            {
                Undo.RecordObject(obj.transform, "Paste Transform");
                obj.transform.localPosition = position;
                obj.transform.localEulerAngles = rotation;
                obj.transform.localScale = scale;
                EditorUtility.SetDirty(obj);
                Debug.Log($"Pasted local transform data to {obj.name}");
            }
        }
        catch (System.FormatException e)
        {
            Debug.LogError($"Failed to parse transform values: {e.Message}");
        }
    }
}