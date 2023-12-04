Shader "SSS/PaletteSwap"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Texture)][Space(10)]
        _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [HideInInspector]_Surface("__surface", Float) = 0.0
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("__SrcBlend", Float) = 1
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("__DstBlend", Float) = 0
        [HideInInspector] _AlphaToMask("__AlphaToMask", Float) = 0.0
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
            Name "PaltteSwap"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull [_CullMode]
            // https://forum.unity.com/threads/is-there-extra-performance-cost-for-blend-one-zero.1154021/
            Blend [_SrcBlend][_DstBlend]
            AlphaToMask [_AlphaToMask]
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // 定数バッファー
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 PositionHCS  : SV_POSITION;
                float2 texcoord     : TEXCOORD0;
                half FogFactor      : TEXCOORD1;
                float3 normalWS     : TEXCOORD3;
                float3 tangentWS    : TEXCOORD4;
                float3 bitangentWS  : TEXCOORD5;
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

                // Position
                OUT.PositionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _BaseMap);

                // Fog
                OUT.FogFactor = ComputeFogFactor(OUT.PositionHCS.z);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                half4 _FinalColor = half4(0.0, 0.0, 0.0, 1.0);
                const half4 _BaseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.texcoord);
                _FinalColor = _BaseTex * _BaseColor;
                
                _FinalColor.rgb = _FinalColor.rgb;
                _FinalColor.rgb = MixFog(_FinalColor.rgb, IN.FogFactor);
                
                return _FinalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "META"
            Tags
            {
                "LightMode" = "Meta"
            }
            Cull Off
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            #pragma vertex vert_meta_CBToon
            #pragma fragment frag_meta_CBToon
            
            #pragma shader_feature_local_fragment _EMISSION_ON
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMask);
            
            CBUFFER_START(UnityPerMaterial)
                half _Cutoff;
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _EmissionMask_ST;
                half4 _EmissionColor;
                float _IsScroll;
                float _SpeedX;
                float _SpeedY;
            CBUFFER_END
            
            struct VertexInput_CBToon
            {
                float3 pos : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV: TEXCOORD1;
            };
            
            struct v2f_meta_CBToon
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f_meta_CBToon vert_meta_CBToon (VertexInput_CBToon v)
            {
                v2f_meta_CBToon o;
                if (unity_MetaVertexControl.x)
                {
                    v.pos.xy = v.lightmapUV * unity_LightmapST.xy + unity_LightmapST.zw;
                    // Dummy for OpenGL
                    v.pos.z = v.pos.z > 0 ? REAL_MIN : 0.0f;
                }
                o.pos = TransformWorldToHClip(v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }
            
            float4 frag_meta_CBToon(v2f_meta_CBToon i): SV_Target
            {
                UnityMetaInput o = (UnityMetaInput)0;
                
                const half3 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                o.Albedo = color * _BaseColor.rgb;
                
                #ifdef _EMISSION_ON
                    o.Emission = SAMPLE_TEXTURE2D(_EmissionMask, sampler_BaseMap, i.uv).r * _EmissionColor;
                #else
                    o.Emission = 0;
                #endif
                
                return UnityMetaFragment(o);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}