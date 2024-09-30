using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Collections;



[ExecuteInEditMode]

public class FadeInOut : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] public static float fadeDuration = .2f;
    private readonly Color black = Color.black;
    private readonly Color transparent = new Color(0, 0, 0, 0); // Black with alpha 0
    public void TriggerFadeIn()
    {
        StartCoroutine(FadeImage(black, transparent));
    }

    public void TriggerFadeOut()
    {
        StartCoroutine(FadeImage(transparent, black));
    }

    private IEnumerator FadeImage(Color fromColor, Color toColor)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            image.color = Color.Lerp(fromColor, toColor, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        image.color = toColor;
    }

    public void SetFadeDuration(float duration)
    {
        fadeDuration = duration;
    }
}
[CustomEditor(typeof(FadeInOut))]
public class FadeInOutEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        FadeInOut fadeInOut = (FadeInOut)target;
        if (GUILayout.Button("Trigger Fade In"))
        {
            fadeInOut.TriggerFadeIn();
        }
        if (GUILayout.Button("Trigger Fade Out"))
        {
            fadeInOut.TriggerFadeOut();
        }
    }
}

