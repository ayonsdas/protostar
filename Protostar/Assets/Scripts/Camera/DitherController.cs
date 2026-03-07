using System;
using System.Collections.Generic;
using UnityEngine;

public class DitherController
{
    public const string DITHER_SHADER_PATH = "Shader Graphs/DitherTransparency";
    private int ditherSize;
    private float fadeSpeed;
    private float fadeAlpha;
    private Dictionary<Renderer, float> ditheredRenderers = new();
    private Dictionary<Renderer, Material[]> originalMaterials = new();

    private MaterialPropertyBlock propertyBlock;

    private Shader ditherShader;

    private static readonly string[] textureProps =
    {
        "_BaseMap",
        "_MetallicGlossMap",
        "_BumpMap",
        "_ParallaxMap",
        "_OcclusionMap",
    };

    // private static readonly string[] colorProps =
    // {
    //     "_BaseColor",
    // };

    private static readonly string[] floatProps =
    {
        "_Smoothness",
        "_Metallic"
    };

    public DitherController(
        int ditherSize = 1,
        float fadeSpeed = 4f,
        float fadeAlpha = 0.2f
    )
    {
        ditherShader = Shader.Find(DITHER_SHADER_PATH);
        this.ditherSize = ditherSize;
        this.fadeSpeed = fadeSpeed;
        this.fadeAlpha = fadeAlpha;

        propertyBlock = new MaterialPropertyBlock();
    }

    ~DitherController()
    {
        foreach (var kvp in originalMaterials)
        {
            if (kvp.Key != null)
            {
                kvp.Key.materials = kvp.Value;
            }
        }

        originalMaterials.Clear();
    }

    public void UpdateDither(HashSet<Renderer> ditherTargets)
    {
        // Add new objects to fade and change materials
        foreach (Renderer r in ditherTargets)
        {
            if (!ditheredRenderers.ContainsKey(r))
            {
                ditheredRenderers.Add(r, 1f);
                ChangeMaterials(r);
            }
        }

        // Update the alpha of all currently faded objects
        List<Renderer> keys = new List<Renderer>(ditheredRenderers.Keys);

        foreach (Renderer r in keys)
        {

            // Determine if the object should be faded based on whether it's currently hit
            bool shouldFade = ditherTargets.Contains(r);
            float targetFade = shouldFade ? fadeAlpha : 1f;

            // Interpolate the current alpha towards the target alpha
            float currentFade = ditheredRenderers[r];
            currentFade = Mathf.MoveTowards(currentFade, targetFade, fadeSpeed * Time.deltaTime);

            ditheredRenderers[r] = currentFade;

            ApplyAlpha(r, currentFade);

            // If the object is fully opaque and shouldn't be faded, remove it from the faded objects list
            if (Mathf.Approximately(currentFade, 1f) && !shouldFade)
            {
                // Restore original material property blocks
                for (int i = 0; i < originalMaterials[r].Length; i++)
                {
                    r.SetPropertyBlock(null, i);
                }

                r.materials = originalMaterials[r];
                originalMaterials.Remove(r);
                ditheredRenderers.Remove(r);
            }
        }
    }

    private void ChangeMaterials(Renderer renderer)
    {
        originalMaterials[renderer] = renderer.sharedMaterials;

        var newMats = new Material[renderer.sharedMaterials.Length];

        for (int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            Material original = renderer.sharedMaterials[i];
            Material dither = new Material(ditherShader);
            dither.SetFloat("_DitherSize", ditherSize);
            dither.SetColor("_EmissionColor", Color.black);

            // Copy texture props to dither shader
            TransferProps(original, dither, textureProps, original.GetTexture, dither.SetTexture);
            // TransferProps(original, dither, colorProps, original.GetColor, dither.SetColor);
            TransferProps(original, dither, floatProps, original.GetFloat, dither.SetFloat);

            newMats[i] = dither;
        }

        renderer.materials = newMats;
    }

    // Helper to copy properties from original material to dither material for each type of property (texture, color, float)
    private void TransferProps<T>(
        Material original,
        Material copy,
        string[] props,
        Func<string, T> getProp,
        Action<string, T> setProp
    )
    {
        foreach (string prop in props)
        {
            if (original.HasProperty(prop) && getProp(prop) != null)
            {
                if (copy.HasProperty(prop))
                    setProp(prop, getProp(prop));
                else
                    Debug.LogWarning($"[CameraOcclusionFade] Dither shader is missing property {prop} that exists on original material {original.name}");
            }
        }
    }

    // Helper method to apply the alpha to a renderer
    private void ApplyAlpha(Renderer r, float alpha)
    {
        Material[] originals = originalMaterials[r];

        for (int i = 0; i < originals.Length; i++)
        {
            Material originalMat = originals[i];

            r.GetPropertyBlock(propertyBlock, i);

            Color color = originalMat.GetColor("_BaseColor");
            color.a = alpha;

            propertyBlock.SetColor("_BaseColor", color);

            // Scale emission intensity by the same alpha so it fades in sync
            if (originalMat.HasProperty("_EmissionColor"))
            {
                // GetColor returns emission in linear space, but the dither shader graph
                // interprets _EmissionColor as gamma, so convert to match URP Lit shader output.
                Color emissionColor = originalMat.GetColor("_EmissionColor").gamma;
                propertyBlock.SetColor("_EmissionColor", emissionColor * alpha);
            }

            r.SetPropertyBlock(propertyBlock, i);
        }
    }
}