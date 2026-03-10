using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class OutlineRenderPass : ScriptableRenderPass
{
    private Material _outlineMaterial;
    private Material _dilateMaterial;
    private Material _blurMaterial;
    private Material _compositeMaterial;

    public void SetOutlineMaterial(Material material)
    {
        _outlineMaterial = material;
    }
    public void SetDilateMaterial(Material material)
    {
        _dilateMaterial = material;
    }
    public void SetBlurMaterial(Material material)
    {
        _blurMaterial = material;
    }
    public void SetCompositeMaterial(Material material)
    {
        _compositeMaterial = material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {

        if (
            _outlineMaterial == null ||
            _dilateMaterial == null ||
            _blurMaterial == null ||
            _compositeMaterial == null
        ) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        // Don't display in editor because this can cause issues
        if (cameraData.cameraType != CameraType.Game)
            return;

        var sourceTexture = resourceData.cameraColor;

        // Outline pass
        var outlineDesc = renderGraph.GetTextureDesc(sourceTexture);
        outlineDesc.name = "OutlineTexture";
        outlineDesc.clearBuffer = true;
        outlineDesc.clearColor = Color.black;
        TextureHandle outlineTexture = renderGraph.CreateTexture(outlineDesc);

        RenderGraphUtils.BlitMaterialParameters outlineBlitParameters = new(
            sourceTexture,
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

        // Dilate Pass
        var dilateDesc = renderGraph.GetTextureDesc(sourceTexture);
        dilateDesc.name = "DilateTexture";
        dilateDesc.clearBuffer = false;
        TextureHandle dilateTexture = renderGraph.CreateTexture(dilateDesc);

        MaterialPropertyBlock dilatePropertyBlock = new MaterialPropertyBlock();
        dilatePropertyBlock.SetVector("_TexelSize", texelSize);

        RenderGraphUtils.BlitMaterialParameters dilateBlitParameters = new(
            outlineTexture,
            dilateTexture,
            _dilateMaterial,
            0,
            dilatePropertyBlock
        );
        renderGraph.AddBlitPass(dilateBlitParameters, passName: "DilatePass");

        // Horizontal Blur Pass 
        var horizontalBlurDesc = renderGraph.GetTextureDesc(sourceTexture);
        horizontalBlurDesc.name = "BlurTexture";
        horizontalBlurDesc.clearBuffer = false;
        TextureHandle horizontalBlurTexture = renderGraph.CreateTexture(horizontalBlurDesc);

        MaterialPropertyBlock horizontalBlurPropertyBlock = new MaterialPropertyBlock();
        horizontalBlurPropertyBlock.SetVector(Shader.PropertyToID("_BlurDirection"), new Vector4(1, 0, 0, 0));
        horizontalBlurPropertyBlock.SetVector("_TexelSize", texelSize);

        RenderGraphUtils.BlitMaterialParameters horizontalBlurBlitParameters = new(
            dilateTexture,
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
        verticalBlurPropertyBlock.SetVector(Shader.PropertyToID("_BlurDirection"), new Vector4(0, 1, 0, 0));
        verticalBlurPropertyBlock.SetVector("_TexelSize", texelSize);

        RenderGraphUtils.BlitMaterialParameters verticalBlurBlitParameters = new(
            horizontalBlurTexture,
            verticalBlurTexture,
            _blurMaterial,
            0,
            verticalBlurPropertyBlock
        );
        using(var builder = renderGraph.AddBlitPass(
            verticalBlurBlitParameters, 
            passName: "VerticalBlurPass", 
            returnBuilder: true
        )) {
            builder.SetGlobalTextureAfterPass(verticalBlurTexture, Shader.PropertyToID("_BlurOutlineTexture"));
        }

        // Composite Pass
        var compositeDesc = renderGraph.GetTextureDesc(sourceTexture);
        compositeDesc.name = "BlurTexture";
        compositeDesc.clearBuffer = false;
        TextureHandle compositeTexture = renderGraph.CreateTexture(compositeDesc);

        RenderGraphUtils.BlitMaterialParameters compositeBlitParameters = new(
            sourceTexture,
            compositeTexture,
            _compositeMaterial,
            0
        );
        renderGraph.AddBlitPass(compositeBlitParameters, passName: "CompositePass");

        resourceData.cameraColor = compositeTexture;
    }
}