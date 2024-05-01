Shader "Dither Gradient/Dither Gradient Custom Lit"
{
    Properties
    {
        _Cutoff  ("Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Texture)][Space(10)]
        _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _ToonFeatherBaseTo1st("Toon Feather", Range(0.0001, 1.0)) = 0.0001
        
        [Header(Bump)][Space(10)]
        [NoScaleOffset][Normal] _BumpMap ("BumpMap", 2D) = "bump" { }
        
        [Header(Emissive)][Space(10)]
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMask("Emission Mask", 2D) = "white" {}
        
        [Header(Outline)][Space(10)]
        [KeywordEnum(NML,POS)] _OUTLINE("Outline Mode", Float) = 0
        _OutlineWidth("Outline Width", float) = 0.0
        [HDR]_OutlineColor("Emission Color", Color) = (0,0,0)
        [Enum(UnityEngine.Rendering.CompareFunction)] _OutlineZTest("ZTest", Float) = 4 // LEqual
        
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
            Name "Forward"
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
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _NORMALMAP_ON
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma multi_compile __ _HEIGHT_FOG
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_EmissionMask);
            
            // 定数バッファー
            CBUFFER_START(UnityPerMaterial)
                half    _Cutoff;
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _ToonFeatherBaseTo1st;
                float4 _EmissionMask_ST;
                half4 _EmissionColor;
                // Outline
                float _OutlineWidth;
                half4 _OutlineColor;
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
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 2);
                float3 normalWS     : TEXCOORD3;
                float3 tangentWS    : TEXCOORD4;
                float3 bitangentWS  : TEXCOORD5;
                float3 PositionWS   : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            half3 SampleNormal(float2 uv, float3 normal, float3 tangent, float3 binormal, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
            {
            #ifdef _NORMALMAP_ON
                half4 n = SAMPLE_TEXTURE2D(bumpMap, sampler_bumpMap, uv);
                #if BUMP_SCALE_NOT_SUPPORTED
                    half3 normalTS = UnpackNormal(n);
                #else
                    half3 normalTS = UnpackNormalScale(n, scale);
                #endif
            #else
                half3 normalTS = half3(0.0h, 0.0h, 1.0h);
            #endif
                half3x3 tangentToWorld = half3x3(tangent, binormal, normal);
                return TransformTangentToWorld(normalTS, tangentToWorld);
            }

            half3 SampleEmission(float2 uv, half3 emissionColor, TEXTURE2D_PARAM(emissionMap, sampler_emissionMap))
            {
            #ifndef _EMISSION_ON
                return 0;
            #else
                return SAMPLE_TEXTURE2D(emissionMap, sampler_emissionMap, uv).rgb * emissionColor;
            #endif
            }
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                ZERO_INITIALIZE(Varyings, OUT);
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                // Position
                OUT.PositionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                // World Position
                OUT.PositionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Normal
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInput.normalWS;
                OUT.tangentWS = normalInput.tangentWS;
                OUT.bitangentWS = normalInput.bitangentWS;
                
                // Light Map
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                
                float2 uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                OUT.texcoord = uv;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                
                half4 _FinalColor = half4(0.0, 0.0, 0.0, 1.0);
                
                const half4 _BaseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.texcoord);
                half3 _ToonLightColor = _BaseTex.rgb;
                // #ifdef _ALPHATEST_ON
                //     _FinalColor.a = ((_BaseTex * _BaseColor).a - _Cutoff) / max(fwidth((_BaseTex * _BaseColor).a), 0.0001) + 0.5;
                // #endif
                
            #ifdef _NORMALMAP_ON
                half3 _Normal = SampleNormal(IN.texcoord, IN.normalWS, IN.tangentWS, IN.bitangentWS, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
            #else
                half3 _Normal = IN.normalWS;
            #endif

                half3 bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, _Normal);
                half3 envColor = bakedGI * _BaseColor.rgb;
                envColor *= 1.8f;
                
                Light mainLight = GetMainLight();
                half3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                _ToonLightColor *= _BaseColor.rgb;
                _ToonLightColor *= attenuatedLightColor; 
                
                // Toon Dark
                const float _ToonDarkPow = 0.75;
                half3 _ToonDarkColor = _ToonLightColor * _ToonDarkPow;
                
                half3 envLightColor = envColor.rgb;
                float envLightIntensity = 0.299*envLightColor.r + 0.587*envLightColor.g + 0.114*envLightColor.b <1 ? (0.299*envLightColor.r + 0.587*envLightColor.g + 0.114*envLightColor.b) : 1;
                
                // Half NdotL
                const float _HalfLambert = 0.5 * dot(_Normal, mainLight.direction) + 0.5;
                // Base Color <-> Dark Color Edge
                // const float _FinalShadowMask = saturate((_ShadowPower1 - _HalfLambert) / _ToonFeatherBaseTo1st);
                const float _FinalShadowMask = saturate((0.5 - _HalfLambert) / _ToonFeatherBaseTo1st);

                float _GI_Intensity = 0.0;
                _FinalColor.rgb = lerp(_ToonLightColor, _ToonDarkColor, _FinalShadowMask);
                _FinalColor.rgb += (envLightColor * envLightIntensity * _GI_Intensity * smoothstep(1,0,envLightIntensity/2));
                
            #ifdef _EMISSION_ON
                _FinalColor.rgb += SampleEmission(IN.texcoord, _EmissionColor, TEXTURE2D_ARGS(_EmissionMask, sampler_BaseMap));
            #endif

                // Height Fog
                #ifdef _HEIGHT_FOG
                _FinalColor.rgb = MixUniformHeightFog(
                    _FinalColor.rgb,
                    _HeightFogColor.rgb,
                    IN.PositionWS,
                    _WorldSpaceCameraPos,
                    _HeightFogDensity,
                    _MaxFogHeight,
                    _HeightFogNoise,
                    IN.texcoord,
                    _HeightFogNoiseSpeedX,
                    _HeightFogNoiseSpeedY,
                    _HeightFogNoisePower
                );
                #endif
                
                return _FinalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}