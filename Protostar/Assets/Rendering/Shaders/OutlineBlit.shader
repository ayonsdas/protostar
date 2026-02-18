Shader "Custom/OutlineBlit"
{   
    Properties
    {
        _EdgeRadius("Edge Radius", Float) = 2
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        #define MAX_EDGE_RADIUS 5
        
        float _EdgeRadius;

        TEXTURE2D(_MaskTexture);
        SAMPLER(sampler_MaskTexture);

        float Mask(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, uv).r;
        }

        float MaskBasedRadius(float2 uv, float2 texelSize)
        {
            int radius = clamp(int(_EdgeRadius), 0, MAX_EDGE_RADIUS);
            // Return early if not in mask
            if(!Mask(uv) > 0)
            {
                return 0;
            }

            float edge = 0;

            for (int i = -radius; i < radius; i++)
            {
                for (int j = -radius; j < radius; j++)
                {
                    if(i == 0 && j == 0) {
                        continue;
                    }
                    float2 sampleUV = uv + texelSize.xy * float2(i, j);
                    float neighborMaskInverse = 1 - Mask(sampleUV);
                    if(neighborMaskInverse > 0) {
                        return 1;
                    }
                }
            }
            return edge;
        }

        float4 Frag (Varyings input) : SV_Target
        {
            float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

            float edge = MaskBasedRadius(input.texcoord, texelSize);
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
