using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

public class Tween
{
    public static IEnumerator New(Vector3 targetPos, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startPos = transform.position;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (transform)
        {
            transform.position = targetPos;
        }
    }

    public static IEnumerator NewScale(Vector3 targetScale, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Vector3 startScale = transform.localScale;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (transform)
        {
            transform.localScale = targetScale;
        }
    }


    public static IEnumerator New(Quaternion targetRot, Transform transform, float tweenTime)
    {
        float startTime = Time.time;

        Quaternion startRot = transform.rotation;

        while (Time.time - startTime <= tweenTime && transform)
        {
            transform.rotation = Quaternion.Lerp(startRot, targetRot, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (transform)
        {
            transform.rotation = targetRot;
        }
    }

    public static IEnumerator New(Color32 targetColor, SpriteRenderer spriteRenderer, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = spriteRenderer.color;

        while (Time.time - startTime <= tweenTime && spriteRenderer)
        {
            spriteRenderer.color = Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (spriteRenderer)
        {
            spriteRenderer.color = targetColor;
        }
    }

    public static IEnumerator New(Color32 targetColor, Tilemap tilemap, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = tilemap.color;

        while (Time.time - startTime <= tweenTime && tilemap)
        {
            tilemap.color = Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1));

            yield return new WaitForFixedUpdate();
        }

        if (tilemap)
        {
            tilemap.color = targetColor;
        }
    }

    public static IEnumerator New(float endVal, Vignette vignette, float tweenTime)
    {
        float startTime = Time.time;

        float startVal = (float)vignette.intensity;

        while (Time.time - startTime < startTime + tweenTime && vignette)
        {
            vignette.intensity.Override(startVal + (Time.time - startTime) / tweenTime * (endVal - startVal));
            yield return new WaitForFixedUpdate();
        }

        if (vignette)
        {
            vignette.intensity.Override(endVal);
        }
    }

    public static IEnumerator New(Color32 targetColor, Vignette vignette, float tweenTime)
    {
        float startTime = Time.time;

        Color32 startColor = (Color32)(Color)vignette.color;

        while (Time.time - startTime < startTime + tweenTime && vignette)
        {
            vignette.color.Override(Color32.Lerp(startColor, targetColor, Mathf.Clamp((Time.time - startTime) / tweenTime, 0, 1)));
            yield return new WaitForFixedUpdate();
        }

        if (vignette)
        {
            vignette.color.Override(targetColor);
        }
    }
}