using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;


public class OutlineRenderPass : ScriptableRenderPass
{
    private Material _outlineMaterial;

    public void SetMaterial(Material material)
    {
        _outlineMaterial = material;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        if (_outlineMaterial == null) return;

        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        var sourceTexture = resourceData.activeColorTexture;

        var destinationDesc = renderGraph.GetTextureDesc(sourceTexture);
        destinationDesc.name = "DestinationTexture";
        destinationDesc.clearBuffer = false;
        TextureHandle destinationTexture = renderGraph.CreateTexture(destinationDesc);

        RenderGraphUtils.BlitMaterialParameters blitParameters = new(
            sourceTexture,
            destinationTexture,
            _outlineMaterial,
            0
        );

        renderGraph.AddBlitPass(blitParameters, passName: "OutlinePass");

        resourceData.cameraColor = destinationTexture;
    }
}