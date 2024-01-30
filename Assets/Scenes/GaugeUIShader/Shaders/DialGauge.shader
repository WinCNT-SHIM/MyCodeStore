Shader "SSS/UI/DialGauge"
{
    Properties
    {
        [Header(Base Settings)][Space(5)]
        _BaseMap ("Base Map", 2D) = "white" { }
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff  ("Cutoff", Range(0.0, 1.0)) = 0.5
        
        [Header(Fill Amount Settings)][Space(5)]
        _FillAmount ("Fill Amount", Range(0, 1)) = 1.0
        [Toggle] _Reverse ("Reverse", float) = 0
        
        _CenterX ("Center X", Range(0, 1)) = 0.5
        _CenterY ("Center Y", Range(0, 1)) = 0.5
        _Rotation ("Rotation Angle", Range(-360, 360)) = 0.0
        _FillRangeAngle ("Fill Range Angle", Range(0.001, 360)) = 360.0
        
        [Header(Advanced Settings)][Space(5)]
        [HideInInspector][Toggle] _ALPHATEST  ("Alpha Test", float) = 1.0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("ZTest", Float) = 4 // LEqual
        [Enum(Off, 0, On, 1)] _ZWrite("ZWrite", Float) = 0 // Off
    }
    
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull [_CullMode]
            ZTest [_ZTest]
            ZWrite [_ZWrite]
            AlphaToMask On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // 定数バッファー
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _FillAmount;
                float _Reverse;
                float  _CenterX;
                float  _CenterY;
                float  _Rotation;
                float  _FillRangeAngle;
                half _Cutoff;
                float4 _BaseMap_ST;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 UV           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 PositionHCS  : SV_POSITION;
                float2 UV           : TEXCOORD0;
                float2 GaugeUV      : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                ZERO_INITIALIZE(Varyings, OUT);
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.PositionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.UV = TRANSFORM_TEX(IN.UV, _BaseMap);

                // Dial Gauge Fill Ammount
                // Rotate
                float2 uv = IN.UV;
                uv.x = lerp(uv.x, 1.0 - uv.x, _Reverse);
                float rotation = _Rotation * (PI/180.0f);
                float2 center = float2(_CenterX, _CenterY);
                float2 rotateUV = uv - center;
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                rotateUV.xy = mul(rotateUV.xy, rMatrix);
                rotateUV += center;
                
                // Polar CoordinatesのDelta(Compute radius and angle at fragment Shader)
                OUT.GaugeUV = rotateUV - center;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                half4 outColor = half4(0.0, 0.0, 0.0, 1.0);
                
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.UV);
                outColor.rgb = baseTex.rgb * _BaseColor.rgb;
                outColor.a = AlphaDiscard(baseTex.a, _Cutoff);
                
                // Polar Coordinates
                float2 delta = IN.GaugeUV;
                // float radius = length(delta);
                float angle = atan2(delta.x, delta.y) * 1.0/6.28;
                
                // Fill Ammount
                const float gaugeAlpha = min(max((angle + 0.5) * (360.0 / _FillRangeAngle), 0.001), 0.999);
                clip(-gaugeAlpha + _FillAmount);
                
                return outColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}