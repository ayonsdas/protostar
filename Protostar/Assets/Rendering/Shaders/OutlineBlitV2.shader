Shader "Custom/OutlineBlitV2"
{   
    Properties
    {
        _DepthThreshold("Depth Edge Threshold", Float) = 0.01
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float _DepthThreshold;
        CBUFFER_END
        
        TEXTURE2D(_MaskTexture);
        SAMPLER(sampler_MaskTexture);

        static const float SobelX[9] = {
            -1, 0, 1,
            -2, 0, 2,
            -1, 0, 1
        };
        static const float SobelY[9] = {
            -1, -2, -1,
            0,  0,  0,
            1,  2,  1
        };
        static const float2 offsets[9] = {
            float2(-1,  1), float2( 0,  1), float2( 1,  1),
            float2(-1,  0), float2( 0,  0), float2( 1,  0),
            float2(-1, -1), float2( 0, -1), float2( 1, -1)
        };

        float3 ViewSpaceNormal(float2 uv)
        {
            float3 normalWS = SampleSceneNormals(uv);

            // Transform from world-space to view-space
            float3 normalVS = mul((float3x3)UNITY_MATRIX_V, normalWS);
            normalVS = normalize(normalVS);

            // Remap from [-1, 1] to [0, 1]
            // normalVS = normalVS * 0.5f + 0.5f;
            return normalVS;
        }

        float Mask(float2 uv)
        {
            return SAMPLE_TEXTURE2D(_MaskTexture, sampler_MaskTexture, uv).r;
        }

        float SobelDepth(float2 uv, float2 texelSize)
        {
            float gx = 0;
            float gy = 0;

            for (int i = 0; i < 9; i++)
            {
                float2 sampleUV = uv + offsets[i] * texelSize;
                float d = SampleSceneDepth(sampleUV);

                gx += d * SobelX[i];
                gy += d * SobelY[i];
            }

            float depthEdge = sqrt(gx*gx + gy*gy);
            depthEdge = depthEdge * 100;
            return step(_DepthThreshold, depthEdge);
        }

        float SobelNormals(float2 uv, float2 texelSize)
        {
            float3 gx = float3(0, 0, 0);
            float3 gy = float3(0, 0, 0);

            for (int i = 0; i < 9; i++)
            {
                float2 sampleUV = uv + offsets[i] * texelSize;
                float3 d = ViewSpaceNormal(sampleUV);

                gx += d * SobelX[i];
                gy += d * SobelY[i];
            }
            float edge = length(gx + gy) / 16;
            edge = saturate(edge);
            return step(0.05, edge);
            return edge;
        }

        float MaskBoxBlur(float2 uv, float2 texelSize, float2 blurDirection, float blurRadius) {
            float mask = 0;
            int radius = int(ceil(blurRadius));
            int samples = 0;

            for (int i = -radius; i <= radius; ++i)
            {
                float2 uvSample = uv + blurDirection.xy * texelSize.xy * float(i);
                float sample = Mask(uvSample);
                mask += sample;
                samples++;
            }

            mask /= samples;
            return mask;
        }

        // float CrossNormals(float2 uv, float2 texelSize)
        // {
        //     float3 d = ViewSpaceNormal(sampleUV);
        //     float difference1 = 0;
        // }

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

            float4 Frag (Varyings input) : SV_Target
            {
                // float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                // return color;
                float2 texelSize = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // float normalEdge = SobelNormals(input.texcoord, texelSize);
                // float depthEdge = SobelDepth(input.texcoord, texelSize);
                // float color = max(normalEdge, depthEdge);
                // return float4(color, color, color, 1);

                float h = MaskBoxBlur(input.texcoord, texelSize, float2(1, 0), 5);
                float v = MaskBoxBlur(input.texcoord, texelSize, float2(0, 1), 5);

                float blurred = (h + v) * 0.5;
                float smoothed = smoothstep(0.02, 0.95, blurred);
                float outside = saturate(smoothed - Mask(input.texcoord));
                return float4(outside, outside, outside, 1);

                float4 outlineColor = float4(1, 0, 0, 1);
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                float4 color = lerp(sceneColor, outlineColor, outside);
                return color;

                // float4 color = SobelNormals(input.texcoord, texelSize);
                // return color;
            }
            
            ENDHLSL
        }
    }
}
