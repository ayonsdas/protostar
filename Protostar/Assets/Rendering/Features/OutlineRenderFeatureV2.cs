using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class OutlineRenderFeatureV2Settings
{
    [Header("Shaders")]
    public Shader MaskShader;
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

public class OutlineRenderFeatureV2 : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private OutlineRenderFeatureV2Settings _settings = new OutlineRenderFeatureV2Settings();
    private MaskRenderPass _maskRenderPass;
    private OutlineRenderPassV2 _outlineRenderPass;
    private Material _maskMaterial;
    private Material _outlineMaterial;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Create outline and normals materials from shaders if not initialized
        if (_maskMaterial == null && _settings.MaskShader != null)
        {
            _maskMaterial = CoreUtils.CreateEngineMaterial(_settings.MaskShader);
            _maskRenderPass?.SetMaterial(_maskMaterial);
        }

        if (_outlineMaterial == null && _settings.OutlineShader != null)
        {
            _outlineMaterial = CoreUtils.CreateEngineMaterial(_settings.OutlineShader);
            _outlineRenderPass?.SetOutlineMaterial(_outlineMaterial);
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
        renderer.EnqueuePass(_maskRenderPass);

        _outlineRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        renderer.EnqueuePass(_outlineRenderPass);
    }

    public override void Create()
    {

        _maskRenderPass = new MaskRenderPass(_settings.OutlineLayer);
        _maskRenderPass.renderPassEvent = _renderPassEvent;

        _outlineRenderPass = new OutlineRenderPassV2();
        _outlineRenderPass.renderPassEvent = _renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_outlineMaterial);

        _maskMaterial = null;
        _outlineMaterial = null;
    }
}