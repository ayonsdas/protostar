using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class OutlineRenderFeatureSettings
{
    [Header("Shaders")]
    public Shader DepthShader;
    public Shader NormalsShader;
    public Shader OutlineShader;
    public RenderingLayerMask OutlineLayer;
    [Header("Outline Settings")]
    public Color OutlineColor;
    [Range(0f, 1f)]
    public float DepthThreshold = 0.5f;

    [Range(0f, 1f)]
    public float NormalThreshold = 0.5f;

    [Range(0f, 2f)]
    public float OutlineScale = 1f;
    [Range(0f, 2f)]
    public float Multiplier = 1f;
}

public class OutlineRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private OutlineRenderFeatureSettings _settings = new OutlineRenderFeatureSettings();
    private MaskedDepthRenderPass _maskedDepthRenderPass;
    private MaskedNormalsRenderPass _maskedNormalsRenderPass;
    private OutlineRenderPass _outlineRenderPass;
    private Material _depthMaterial;
    private Material _normalsMaterial;
    private Material _outlineMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Create outline and normals materials from shaders if not initialized
        if (_depthMaterial == null && _settings.DepthShader != null)
        {
            _depthMaterial = CoreUtils.CreateEngineMaterial(_settings.DepthShader);
            _maskedDepthRenderPass?.SetMaterial(_depthMaterial);
        }

        if (_normalsMaterial == null && _settings.NormalsShader != null)
        {
            _normalsMaterial = CoreUtils.CreateEngineMaterial(_settings.NormalsShader);
            _maskedNormalsRenderPass?.SetMaterial(_normalsMaterial);
        }

        if (_outlineMaterial == null && _settings.OutlineShader != null)
        {
            _outlineMaterial = CoreUtils.CreateEngineMaterial(_settings.OutlineShader);
            _outlineRenderPass?.SetMaterial(_outlineMaterial);
        }

        // Update shader settings from serialize properties
        if (_outlineMaterial != null)
        {
            _outlineMaterial.SetColor("_OutlineColor", _settings.OutlineColor);
            _outlineMaterial.SetFloat("_OutlineScale", _settings.OutlineScale);
            _outlineMaterial.SetFloat("_DepthThreshold", _settings.DepthThreshold);
            _outlineMaterial.SetFloat("_NormalThreshold", _settings.NormalThreshold);
            _outlineMaterial.SetFloat("_RobertsCrossMultiplier", _settings.Multiplier);
        }

        // Set main passes
        renderer.EnqueuePass(_maskedDepthRenderPass);
        renderer.EnqueuePass(_maskedNormalsRenderPass);
        renderer.EnqueuePass(_outlineRenderPass);
    }

    public override void Create()
    {
        _maskedDepthRenderPass = new MaskedDepthRenderPass(_settings.OutlineLayer);
        _maskedDepthRenderPass.renderPassEvent = _renderPassEvent;

        _maskedNormalsRenderPass = new MaskedNormalsRenderPass(_settings.OutlineLayer);
        _maskedNormalsRenderPass.renderPassEvent = _renderPassEvent;

        _outlineRenderPass = new OutlineRenderPass();
        _outlineRenderPass.renderPassEvent = _renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_depthMaterial);
        CoreUtils.Destroy(_normalsMaterial);
        CoreUtils.Destroy(_outlineMaterial);

        _depthMaterial = null;
        _normalsMaterial = null;
        _outlineMaterial = null;
    }
}