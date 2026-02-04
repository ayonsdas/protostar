using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class OutlineRenderFeatureSettings
{
    [Header("Shaders")]
    public Shader MaskShader;
    public Shader OutlineShader;
    public Shader DilateShader;
    public Shader BlurShader;
    public Shader CompositeShader;
    public RenderingLayerMask OutlineLayer;
    [Header("Outline Settings")]
    public Color OutlineColor;
    [Range(0f, 5f)]
    public float EdgeRadius = 2f;
    [Range(0f, 5f)]
    public float DilateRadius = 2f;
    [Range(0f, 5f)]
    public float BlurRadius = 2f;
}

public class OutlineRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private RenderPassEvent _renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    [SerializeField] private OutlineRenderFeatureSettings _settings = new OutlineRenderFeatureSettings();
    private MaskRenderPass _maskRenderPass;
    private OutlineRenderPass _outlineRenderPass;
    private Material _maskMaterial;
    private Material _outlineMaterial;
    private Material _dilateMaterial;
    private Material _blurMaterial;
    private Material _compositeMaterial;

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

        if (_dilateMaterial == null && _settings.DilateShader != null)
        {
            _dilateMaterial = CoreUtils.CreateEngineMaterial(_settings.DilateShader);
            _outlineRenderPass?.SetDilateMaterial(_dilateMaterial);
        }

        if (_blurMaterial == null && _settings.BlurShader != null)
        {
            _blurMaterial = CoreUtils.CreateEngineMaterial(_settings.BlurShader);
            _outlineRenderPass?.SetBlurMaterial(_blurMaterial);
        }

        if (_compositeMaterial == null && _settings.CompositeShader != null)
        {
            _compositeMaterial = CoreUtils.CreateEngineMaterial(_settings.CompositeShader);
            _outlineRenderPass?.SetCompositeMaterial(_compositeMaterial);
        }

        // Update shader settings from serialize properties
        if (_outlineMaterial != null)
        {
            _outlineMaterial.SetFloat("_EdgeRadius", _settings.EdgeRadius);
        }
        if (_outlineMaterial != null)
        {
            _outlineMaterial.SetFloat("_DilateRadius", _settings.DilateRadius);
        }
        if (_blurMaterial != null)
        {
            _blurMaterial.SetFloat("_BlurRadius", _settings.BlurRadius);
        }
        if (_compositeMaterial != null)
        {
            _compositeMaterial.SetColor("_OutlineColor", _settings.OutlineColor);
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

        _outlineRenderPass = new OutlineRenderPass();
        _outlineRenderPass.renderPassEvent = _renderPassEvent;
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_maskMaterial);
        CoreUtils.Destroy(_outlineMaterial);
        CoreUtils.Destroy(_dilateMaterial);
        CoreUtils.Destroy(_blurMaterial);
        CoreUtils.Destroy(_compositeMaterial);

        _maskMaterial = null;
        _outlineMaterial = null;
        _dilateMaterial = null;
        _blurMaterial = null;
        _compositeMaterial = null;
    }
}