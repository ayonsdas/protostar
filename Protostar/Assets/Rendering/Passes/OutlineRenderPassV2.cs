using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


public class OutlineRenderPassV2 : ScriptableRenderPass
{
    private Material _outlineMaterial;

    public void SetOutlineMaterial(Material material)
    {
        _outlineMaterial = material;
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

        RenderGraphUtils.BlitMaterialParameters outlineBlitParameters = new(
            sourceTexture,
            outlineTexture,
            _outlineMaterial,
            0
        );
        renderGraph.AddBlitPass(outlineBlitParameters, passName: "OutlinePass");

        resourceData.cameraColor = outlineTexture;
    }
}