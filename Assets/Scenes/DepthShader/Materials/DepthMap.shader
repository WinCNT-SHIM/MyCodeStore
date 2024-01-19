Shader "PV/DepthMap"
{
    Properties
    {
        [Header(Input Level)]
        [Space(5)]
        _BlackInputLevel("Black Input Level", Range(0.0, 253.0)) = 0.0
        _WhiteInputLevel("White Input Level", Range(2.0, 255.0)) = 255
        [PowerSlider(110)] _Gamma("Gamma", Range(9.9, 0.1)) = 1.0
        
//        [Header(Output Level)]
//        [Space(5)]
//        _BlackOutputLevel("Black Output Level", Range(0.0, 255.0)) = 0.0
//        _WhiteOutputLevel("White Output Level", Range(0.0, 255.0)) = 255
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _BlackInputLevel;
            float _WhiteInputLevel;
            float _Gamma;
            float _BlackOutputLevel;
            float _WhiteOutputLevel;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 UV = IN.positionHCS.xy / _ScaledScreenParams.xy;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                const float blackInputLevel = _BlackInputLevel / 255;
                const float whiteInputLevel = _WhiteInputLevel / 255;
                const float gamma = _Gamma;
                const float blackOutputLevel = _BlackOutputLevel / 255;
                const float whiteOutputLevel = _WhiteOutputLevel / 255;
                
                // Reverse Gamma Correction
                depth = pow(depth, 1/2.2);
                
                depth = (depth - blackInputLevel) / (whiteInputLevel - blackInputLevel);
                depth = pow(depth, 1.0 / gamma);

                // const float blackOutputDepth = saturate(depth + blackOutputLevel);
                // const float whiteOutputDepth = saturate(depth + (whiteOutputLevel - 1.0));

                // depth = saturate(depth + (whiteOutputLevel - 1.0));
                // depth = saturate(depth + blackOutputLevel);
                
                // depth = (blackOutputDepth + whiteOutputDepth) / 2;
                // depth = whiteOutputDepth;
                
                // Reverse Gamma Correction
                depth = pow(depth, 2.2);
                
                return half4(depth,depth,depth,1);
            }
            ENDHLSL
        }
    }
}