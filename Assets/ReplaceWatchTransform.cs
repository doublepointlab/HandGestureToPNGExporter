using UnityEngine;

public class ReplaceWatchTransform : MonoBehaviour
{
    [SerializeField] private Transform watch = null;
    [SerializeField] private Vector3 newPosition = Vector3.zero;
    [SerializeField] private Vector3 newRotation = Vector3.zero;
    [SerializeField] private Vector3 newScale = Vector3.one;
    [SerializeField] private bool replace = false;

    void OnValidate()
    {
        if (watch == null)
        {
            return;
        }

        if (replace)
        {
            ApplyNewTransform();
        }
        else
        {
            ResetToDefault();
        }
    }

    private void ApplyNewTransform()
    {
        if (watch == null)
        {
            Debug.LogWarning("Watch transform is null.");
            return;
        }
        watch.localPosition = newPosition;
        watch.localRotation = Quaternion.Euler(newRotation);
        watch.localScale = newScale;
    }

    private void ResetToDefault()
    {
        if (watch == null)
        {
            Debug.LogWarning("Watch transform is null.");
            return;
        }
        watch.localPosition = Vector3.zero;
        watch.localRotation = Quaternion.identity;
        watch.localScale = Vector3.one;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ReplaceWatchTransform))]
    public class ReplaceWatchTransformEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ReplaceWatchTransform script = (ReplaceWatchTransform)target;
            if (GUILayout.Button("Get Transform Values"))
            {
                if (script.watch != null)
                {
                    script.newPosition = script.watch.localPosition;
                    script.newRotation = script.watch.localRotation.eulerAngles;
                    script.newScale = script.watch.localScale;
                }
                else
                {
                    Debug.LogWarning("Watch transform is null.");
                }
            }
        }
    }
#endif
}
