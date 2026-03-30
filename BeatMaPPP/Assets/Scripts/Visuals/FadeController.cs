// This script controls the fade in and fade out of the prefabs wen they spawn in/destroy
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FadeController : MonoBehaviour
{
    private List<(Renderer renderer, Color[] originalColors)> _rendererData = new();

    public void FadeIn(float duration)
    {
        CollectRenderers();
        StartCoroutine(FadeCoroutine(0f, 1f, duration));
    }

    public void FadeOut(float duration)
    {
        StartCoroutine(FadeCoroutine(1f, 0f, duration));
    }

    // Waits 'delay' seconds, then fades out over 'duration' seconds
    public void ScheduleFadeOut(float delay, float duration)
    {
        StartCoroutine(ScheduledFadeOutCoroutine(delay, duration));
    }

    private IEnumerator ScheduledFadeOutCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        yield return FadeCoroutine(1f, 0f, duration);
    }

    private void CollectRenderers()
    {
        _rendererData.Clear();

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            Color[] colors = new Color[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
            {
                Material mat = rend.materials[i];
                SetMaterialToFadeMode(mat);
                colors[i] = mat.color;
            }
            _rendererData.Add((rend, colors));
        }
    }

    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(endAlpha);
    }

    private void SetAlpha(float alpha)
    {
        foreach (var (rend, originalColors) in _rendererData)
        {
            for (int i = 0; i < rend.materials.Length; i++)
            {
                Color c = originalColors[i];
                c.a = alpha;
                rend.materials[i].color = c;
            }
        }
    }

    private void SetMaterialToFadeMode(Material mat)
    {
        mat.SetFloat("_Surface", 1f);         // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Blend", 0f);           // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_AlphaClip", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}