using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class MaskPassData
{
    public TextureHandle maskTexture;
    public RendererListHandle rendererList;
}

public class MaskRenderPass : ScriptableRenderPass
{
    private FilteringSettings _filteringSettings;
    private int globalMaskTextureID = Shader.PropertyToID("_MaskTexture");
    private Material _maskMaterial;

    public void SetMaterial(Material material)
    {
        _maskMaterial = material;
    }

    public MaskRenderPass(RenderingLayerMask layerMask)
    {
        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: layerMask);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        // Don't display in editor because this can cause issues
        if (cameraData.cameraType != CameraType.Game)
            return;

        using var builder = renderGraph.AddRasterRenderPass<MaskPassData>(
            "Mask Pass",
            out var passData
        );

        // Create Output texture for storing the mask
        TextureDesc desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        desc.colorFormat = GraphicsFormat.R8_UNorm;
        desc.depthBufferBits = DepthBits.None;
        desc.clearBuffer = true;
        desc.clearColor = Color.black;

        passData.maskTexture = renderGraph.CreateTexture(desc);

        builder.SetRenderAttachment(passData.maskTexture, 0);

        // For testing by passing to color instead of output texture
        // builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

        // Use depth to enable occlusion
        //builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

        // Filter to desired layers and use Normals shader material
        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
            new ShaderTagId("UniversalForward"),
            renderingData,
            cameraData,
            lightData,
            SortingCriteria.CommonOpaque
        );
        drawingSettings.overrideMaterial = _maskMaterial;

        RendererListParams rendererListParams = new RendererListParams(
            renderingData.cullResults,
            drawingSettings,
            _filteringSettings
        );

        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);

        // Render function
        builder.SetRenderFunc((MaskPassData data, RasterGraphContext ctx) =>
        {
            ctx.cmd.DrawRendererList(data.rendererList);
        });

        builder.SetGlobalTextureAfterPass(passData.maskTexture, globalMaskTextureID);
    }
}