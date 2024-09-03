using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class HandTransformExporter : MonoBehaviour
{
    [ContextMenu("Export Transforms to CSV")]
    public void ExportTransformsToCSV()
    {
        List<Transform> relevantTransforms = GetRelevantChildTransforms(transform);
        string csvContent = "Model,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,ScaleX,ScaleY,ScaleZ\n";

        foreach (Transform t in relevantTransforms)
        {
            string line = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11}\n",
                0, // Model index, you can change this if needed
                t.name,
                t.localPosition.x, t.localPosition.y, t.localPosition.z,
                t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w,
                t.localScale.x, t.localScale.y, t.localScale.z);
            csvContent += line;
        }

        string fileName = gameObject.name + "_Transforms.csv";
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(filePath, csvContent);
        Debug.Log("Transforms exported to " + filePath);
    }

    private List<Transform> GetRelevantChildTransforms(Transform parent)
    {
        List<Transform> transforms = new List<Transform>();
        foreach (Transform child in parent)
        {
            if (child.childCount > 0)
            {
                transforms.Add(child);
                transforms.AddRange(GetRelevantChildTransforms(child));
            }
        }
        return transforms;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(HandTransformExporter))]
public class HandTransformExporterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HandTransformExporter exporter = (HandTransformExporter)target;
        if (GUILayout.Button("Export Transforms to CSV"))
        {
            exporter.ExportTransformsToCSV();
        }
    }
}
#endif