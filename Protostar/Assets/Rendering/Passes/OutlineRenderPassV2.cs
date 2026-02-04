using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


public class OutlineRenderPassV2 : ScriptableRenderPass
{
    private Material _outlineMaterial;
    private Material _blurMaterial;

    public void SetOutlineMaterial(Material material)
    {
        _outlineMaterial = material;
    }
    public void SetBlurMaterial(Material material)
    {
        _blurMaterial = material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_outlineMaterial == null) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        var sourceTexture = resourceData.cameraColor;

        // Outline pass
        var outlineDesc = renderGraph.GetTextureDesc(sourceTexture);
        outlineDesc.name = "OutlineTexture";
        outlineDesc.clearBuffer = false;
        TextureHandle outlineTexture = renderGraph.CreateTexture(outlineDesc);

        RenderGraphUtils.BlitMaterialParameters outlineBlitParameters = new(
            TextureHandle.nullHandle,
            outlineTexture,
            _outlineMaterial,
            0
        );
        renderGraph.AddBlitPass(outlineBlitParameters, passName: "OutlinePass");

        Vector4 texelSize = new Vector4(
            1f / outlineDesc.width,
            1f / outlineDesc.height,
            outlineDesc.width,
            outlineDesc.height
        );

        // Horizontal Blur Pass 
        var horizontalBlurDesc = renderGraph.GetTextureDesc(sourceTexture);
        horizontalBlurDesc.name = "BlurTexture";
        horizontalBlurDesc.clearBuffer = false;
        TextureHandle horizontalBlurTexture = renderGraph.CreateTexture(horizontalBlurDesc);

        MaterialPropertyBlock horizontalBlurPropertyBlock = new MaterialPropertyBlock();
        horizontalBlurPropertyBlock.SetFloat(Shader.PropertyToID("_BlurRadius"), 1);
        horizontalBlurPropertyBlock.SetVector(Shader.PropertyToID("_BlurDirection"), new Vector4(1, 0, 0, 0));
        horizontalBlurPropertyBlock.SetVector("_TexelSize", texelSize);

        RenderGraphUtils.BlitMaterialParameters horizontalBlurBlitParameters = new(
            outlineTexture,
            horizontalBlurTexture,
            _blurMaterial,
            0,
            horizontalBlurPropertyBlock
        );
        renderGraph.AddBlitPass(horizontalBlurBlitParameters, passName: "HorizontalBlurPass");

        // Vertical Blur Pass
        var verticalBlurDesc = renderGraph.GetTextureDesc(sourceTexture);
        verticalBlurDesc.name = "BlurTexture";
        verticalBlurDesc.clearBuffer = false;
        TextureHandle verticalBlurTexture = renderGraph.CreateTexture(verticalBlurDesc);

        MaterialPropertyBlock verticalBlurPropertyBlock = new MaterialPropertyBlock();
        verticalBlurPropertyBlock.SetFloat(Shader.PropertyToID("_BlurRadius"), 1);
        verticalBlurPropertyBlock.SetVector(Shader.PropertyToID("_BlurDirection"), new Vector4(0, 1, 0, 0));
        verticalBlurPropertyBlock.SetVector("_TexelSize", texelSize);

        RenderGraphUtils.BlitMaterialParameters verticalBlurBlitParameters = new(
            horizontalBlurTexture,
            verticalBlurTexture,
            _blurMaterial,
            0,
            verticalBlurPropertyBlock
        );
        renderGraph.AddBlitPass(verticalBlurBlitParameters, passName: "VerticalBlurPass");

        resourceData.cameraColor = verticalBlurTexture;
    }
}