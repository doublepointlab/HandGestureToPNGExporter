using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class ApplyTransformToObjects : MonoBehaviour
{
    [SerializeField] private string tagToApply = "Watch";
    [HideInInspector] public GameObject[] objects;

    public void ApplyTransform()
    {
        objects = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.CompareTag(tagToApply)).ToArray();
        Transform thisTransform = GetComponent<Transform>();
        
        foreach (GameObject obj in objects)
        {
            obj.transform.localPosition = thisTransform.localPosition;
            obj.transform.localRotation = thisTransform.localRotation;
            obj.transform.localScale = thisTransform.localScale;
        }
    }
}

[CustomEditor(typeof(ApplyTransformToObjects))]
public class ApplyTransformToObjectsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ApplyTransformToObjects script = (ApplyTransformToObjects)target;
        if (GUILayout.Button($"Apply Transform To Objects with Tag"))
        {
            script.ApplyTransform();
            Debug.Log("Transform applied to " + script.objects.Length + " objects.");
        }
    }
}
