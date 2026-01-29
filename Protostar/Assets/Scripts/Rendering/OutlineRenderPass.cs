using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


public class OutlineRenderPass : ScriptableRenderPass
{
    private const float BLUR_RADIUS = 1f;
    private Material _outlineMaterial;
    private Material _dilateMaterial;
    private Material _erodeMaterial;
    private Material _compositeMaterial;

    public void SetOutlineMaterial(Material material)
    {
        _outlineMaterial = material;
    }
    public void SetDilateMaterial(Material material)
    {
        _dilateMaterial = material;
    }
    public void SetErodeMaterial(Material material)
    {
        _erodeMaterial = material;
    }
    public void SetCompositeMaterial(Material material)
    {
        _compositeMaterial = material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_outlineMaterial == null) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        var sourceTexture = resourceData.activeColorTexture;


        var outlineDesc = renderGraph.GetTextureDesc(sourceTexture);
        outlineDesc.name = "OutlineTexture";
        outlineDesc.clearBuffer = false;
        TextureHandle outlineTexture = renderGraph.CreateTexture(outlineDesc);

        // var dilateDesc = renderGraph.GetTextureDesc(sourceTexture);
        // dilateDesc.name = "DilateTexture";
        // dilateDesc.clearBuffer = false;
        // TextureHandle dilateTexture = renderGraph.CreateTexture(dilateDesc);

        // var erodeDesc = renderGraph.GetTextureDesc(sourceTexture);
        // erodeDesc.name = "ErodeTexture";
        // erodeDesc.clearBuffer = false;
        // TextureHandle erodeTexture = renderGraph.CreateTexture(erodeDesc);

        // var compositeDesc = renderGraph.GetTextureDesc(sourceTexture);
        // compositeDesc.name = "CompositeTexture";
        // compositeDesc.clearBuffer = false;
        // TextureHandle compositeTexture = renderGraph.CreateTexture(compositeDesc);

        RenderGraphUtils.BlitMaterialParameters outlineBlitParameters = new(
            sourceTexture,
            outlineTexture,
            _outlineMaterial,
            0
        );
        renderGraph.AddBlitPass(outlineBlitParameters, passName: "OutlinePass");

        // int width = renderGraph.GetTextureDesc(outlineTexture).width;
        // int height = renderGraph.GetTextureDesc(outlineTexture).height;

        // MaterialPropertyBlock dilatePropertyBlock = new MaterialPropertyBlock();
        // dilatePropertyBlock.SetFloat("_BlurRadius", BLUR_RADIUS);
        // dilatePropertyBlock.SetVector("_TexelSize", new Vector4(1f / width, 1f / height, 0, 0));

        // RenderGraphUtils.BlitMaterialParameters dilateBlitParameters = new(
        //     outlineTexture,
        //     dilateTexture,
        //     _dilateMaterial,
        //     0,
        //     dilatePropertyBlock
        // );
        // renderGraph.AddBlitPass(dilateBlitParameters, passName: "DilatePass");

        // MaterialPropertyBlock erodePropertyBlock = new MaterialPropertyBlock();
        // erodePropertyBlock.SetFloat("_BlurRadius", BLUR_RADIUS);
        // erodePropertyBlock.SetVector("_TexelSize", new Vector4(1f / width, 1f / height, 0, 0));

        // RenderGraphUtils.BlitMaterialParameters erodeBlitParameters = new(
        //     dilateTexture,
        //     erodeTexture,
        //     _erodeMaterial,
        //     0,
        //     erodePropertyBlock
        // );
        // renderGraph.AddBlitPass(erodeBlitParameters, passName: "ErodePass");

        // RenderGraphUtils.BlitMaterialParameters compositeBlitParameters = new(
        //     erodeTexture,
        //     compositeTexture,
        //     _compositeMaterial,
        //     0
        // );
        // using (var builder = renderGraph.AddBlitPass(compositeBlitParameters, passName: "CompositePass", returnBuilder: true))
        // {
        //     builder.UseGlobalTexture(sceneColorTextureID);
        // }

        // resourceData.cameraColor = compositeTexture;
        resourceData.cameraColor = outlineTexture;
    }
}