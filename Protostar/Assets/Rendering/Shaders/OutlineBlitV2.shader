Shader "Custom/OutlineBlitV2"
{   
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        
        TEXTURE2D(_MaskTexture);
        SAMPLER(sampler_MaskTexture);

        static const float2 offsets[8] = {
            float2( 1, 0), float2(-1, 0),
            float2( 0, 1), float2( 0,-1),
            float2( 1, 1), float2(-1, 1),
            float2( 1,-1), float2(-1,-1)
        };

        float Mask(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, uv).r;
        }

        float MaskBased(float2 uv, float2 texelSize)
        {
            // Return early if not in mask
            if(!Mask(uv) > 0)
            {
                return 0;
            }

            float edge = 0;

            for (int i = 0; i < 8; i++)
            {
                float2 sampleUV = uv + texelSize.xy * offsets[i];
                float neighborMaskInverse = 1 - Mask(sampleUV);
                edge = max(edge, neighborMaskInverse);
            }
            return edge;
        }

        float4 Frag (Varyings input) : SV_Target
        {
            float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

            float edge = MaskBased(input.texcoord, texelSize);
            return float4(edge, edge, edge, 1);
        }

        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "OutlineBlitV2"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            ENDHLSL
        }
    }
}
