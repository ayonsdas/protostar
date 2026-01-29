using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class OutlineRenderFeatureSettings
{
    [Header("Shaders")]
    public Shader NormalsShader;
    public Shader OutlineShader;
    public Shader DilateShader;
    public Shader ErodeShader;
    public Shader CompositeShader;
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
    private MaskedNormalsRenderPass _maskedNormalsRenderPass;
    private OutlineRenderPass _outlineRenderPass;
    private Material _normalsMaterial;
    private Material _outlineMaterial;
    private Material _dilateMaterial;
    private Material _erodeMaterial;
    private Material _compositeMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Create outline and normals materials from shaders if not initialized
        if (_normalsMaterial == null && _settings.NormalsShader != null)
        {
            _normalsMaterial = CoreUtils.CreateEngineMaterial(_settings.NormalsShader);
            _maskedNormalsRenderPass?.SetMaterial(_normalsMaterial);
        }

        if (_outlineMaterial == null && _settings.OutlineShader != null)
        {
            _outlineMaterial = CoreUtils.CreateEngineMaterial(_settings.OutlineShader);
            _outlineRenderPass?.SetOutlineMaterial(_outlineMaterial);
        }

        if (_dilateMaterial == null && _settings.DilateShader != null)
        {
            _dilateMaterial = CoreUtils.CreateEngineMaterial(_settings.DilateShader);
            _outlineRenderPass?.SetDilateMaterial(_dilateMaterial);
        }

        if (_erodeMaterial == null && _settings.ErodeShader != null)
        {
            _erodeMaterial = CoreUtils.CreateEngineMaterial(_settings.ErodeShader);
            _outlineRenderPass?.SetErodeMaterial(_erodeMaterial);
        }

        if (_compositeMaterial == null && _settings.CompositeShader != null)
        {
            _compositeMaterial = CoreUtils.CreateEngineMaterial(_settings.CompositeShader);
            _outlineRenderPass?.SetCompositeMaterial(_compositeMaterial);
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
        renderer.EnqueuePass(_maskedNormalsRenderPass);
        renderer.EnqueuePass(_outlineRenderPass);
    }

    public override void Create()
    {

        _maskedNormalsRenderPass = new MaskedNormalsRenderPass(_settings.OutlineLayer);
        _maskedNormalsRenderPass.renderPassEvent = _renderPassEvent;

        _outlineRenderPass = new OutlineRenderPass();
        _outlineRenderPass.renderPassEvent = _renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_normalsMaterial);
        CoreUtils.Destroy(_outlineMaterial);
        CoreUtils.Destroy(_dilateMaterial);
        CoreUtils.Destroy(_erodeMaterial);

        _normalsMaterial = null;
        _outlineMaterial = null;
        _dilateMaterial = null;
        _erodeMaterial = null;
    }
}