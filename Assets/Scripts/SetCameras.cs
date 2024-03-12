using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetCameras : MonoBehaviour
{
        public GameObject[] hands;
        public Camera cameraPrefab;
        public Vector3 camOffsetPosition;

        public void InstantiateCameras()
        {
            hands = GameObject.FindGameObjectsWithTag("Hand");

            // Delete existing cameras before instantiating new ones
            var existingCameras = GameObject.FindGameObjectsWithTag("HandCamera");
            foreach (var existingCamera in existingCameras)
            {
                DestroyImmediate(existingCamera);
            }

            foreach (var hand in hands)
            {

                Camera cam = Instantiate(cameraPrefab, hand.transform);
                cam.gameObject.tag = "HandCamera"; // Ensure the instantiated camera has the correct tag
                cam.name = "HandCamera";
                cam.transform.localPosition = camOffsetPosition;
            }

        }

}

#if UNITY_EDITOR
    [CustomEditor(typeof(SetCameras))]
    public class SetCamerasEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            SetCameras setCamerasScript = (SetCameras)target;
            setCamerasScript.InstantiateCameras();

            // Export frame button
            if (GUILayout.Button("Export All Frames"))
            {
                GameObject[] handCameras = GameObject.FindGameObjectsWithTag("HandCamera");
                foreach (var handCamera in handCameras)
                {
                    FrameExporter frameExporter = handCamera.GetComponent<FrameExporter>();
                    if (frameExporter != null)
                    {
                        frameExporter.ExportFrame();
                    }
                }
            }
            
            // if (GUILayout.Button("Instantiate Cameras"))
            // {
            //     setCamerasScript.InstantiateCameras();
            // }
            
            // if (GUILayout.Button("Destroy Cameras"))
            // {
            //     var existingCameras = GameObject.FindGameObjectsWithTag("HandCamera");
            //     foreach (var existingCamera in existingCameras)
            //     {
            //         DestroyImmediate(existingCamera);
            //     }
            // }
            


        }


          

    }
#endif