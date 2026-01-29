Shader "Custom/DilateBlit"
{
    Properties
    {
		_BlurRadius("Blur Radius", Float) = 1
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _BlurRadius;
        float4 _TexelSize;

        struct FragInput
        {
            float4 positionHCS : SV_POSITION;
            float2 uv : TEXCOORD0;
        };

        float4 Frag(FragInput input) : SV_Target
        {
            float mask = 0;
            int radius = int(ceil(_BlurRadius));

            for (int i = -radius; i <= radius; ++i)
            {
                for (int j = -radius; j <= radius; ++j) {
                    float2 uvSample = input.uv + _TexelSize.xy * float2(i, j);
                    float sample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvSample).r;
                    mask = max(mask, sample);
                }
            }

            return float4(mask, mask, mask, 1.0);
        }

        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "DilatePass"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
}
