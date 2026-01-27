using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

class NormalMaskPassData
{
    public TextureHandle maskTexture;
    public RendererListHandle rendererList;
}

public class NormalMaskRenderPass : ScriptableRenderPass
{
    public TextureHandle OutputMaskTexture;
    private FilteringSettings _filteringSettings;
    private int globalMaskTextureID = Shader.PropertyToID("_MaskTexture");
    private Material _normalsMaterial;

    public void SetMaterial(Material material)
    {
        _normalsMaterial = material;
    }

    public NormalMaskRenderPass(RenderingLayerMask layerMask)
    {
        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, renderingLayerMask: layerMask);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();

        using var builder = renderGraph.AddRasterRenderPass<NormalMaskPassData>(
            "Normal Mask Pass",
            out var passData
        );

        // Create Output texture for storing the mask
        TextureDesc desc = renderGraph.GetTextureDesc(resourceData.activeColorTexture);
        desc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
        desc.depthBufferBits = DepthBits.None;
        desc.clearBuffer = true;
        desc.clearColor = Color.black;

        passData.maskTexture = renderGraph.CreateTexture(desc);

        builder.SetRenderAttachment(passData.maskTexture, 0);

        // For testing by passing to color instead of output texture
        // builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

        // Use depth to enable occlusion
        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

        // Filter to desired layers and use Normals shader material
        DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
            new ShaderTagId("UniversalForward"),
            renderingData,
            cameraData,
            lightData,
            SortingCriteria.CommonOpaque
        );
        drawingSettings.overrideMaterial = _normalsMaterial;

        RendererListParams rendererListParams = new RendererListParams(
            renderingData.cullResults,
            drawingSettings,
            _filteringSettings
        );

        passData.rendererList = renderGraph.CreateRendererList(rendererListParams);
        builder.UseRendererList(passData.rendererList);

        // Render function
        builder.SetRenderFunc((NormalMaskPassData data, RasterGraphContext ctx) =>
        {
            ctx.cmd.DrawRendererList(data.rendererList);
        });

        builder.SetGlobalTextureAfterPass(passData.maskTexture, globalMaskTextureID);
        OutputMaskTexture = passData.maskTexture;
    }
}