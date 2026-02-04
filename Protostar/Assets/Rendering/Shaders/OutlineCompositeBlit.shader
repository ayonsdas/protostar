Shader "Custom/OutlineCompositeBlit"
{   
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _OutlineColor;

        TEXTURE2D(_BlurOutlineTexture);
        SAMPLER(sampler_BlurOutlineTexture);

        TEXTURE2D(_MaskTexture);
        SAMPLER(sampler_MaskTexture);

        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "OutlineCompositeBlit"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float4 Frag (Varyings input) : SV_Target
            {
                float outlineMask =  SAMPLE_TEXTURE2D(_BlurOutlineTexture, sampler_BlurOutlineTexture, input.texcoord).r;
                float mask = SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, input.texcoord).r;
                outlineMask = outlineMask * (1 - mask);
                
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                return lerp(sceneColor, _OutlineColor, outlineMask);
            }
            
            ENDHLSL
        }
    }
}
