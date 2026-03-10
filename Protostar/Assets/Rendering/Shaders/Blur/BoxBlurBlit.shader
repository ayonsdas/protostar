Shader "Custom/BoxBlurBlit"
{
    Properties
    {
		_BlurRadius("Blur Radius", Float) = 3
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurRadius;
        float4 _TexelSize;
        float4 _BlurDirection;

        struct VertexInput
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct FragInput
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };
        
        float4 Frag(FragInput input) : SV_Target
        {
            float mask = 0;
            int radius = int(ceil(_BlurRadius));
            int samples = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                float2 uvSample = input.uv + _BlurDirection.xy * _TexelSize.xy * float(i);
                float sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvSample).r;
                mask += sample;
                samples++;
            }

            mask /= samples;
            return float4(mask, mask, mask, 1.0);
        }

        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "BlurPass"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
}
