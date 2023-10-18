Shader "SSS/DevelopmentDetail"
{
    Properties
    {
        _Cutoff  ("Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Texture)][Space(10)]
        _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [Header(Bump)][Space(10)]
        [NoScaleOffset][Normal] _BumpMap ("BumpMap", 2D) = "bump" { }
        
        [Header(Emissive)][Space(10)]
        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0)
        _EmissionMask("Emission Mask", 2D) = "white" {}
        
        [Header(UV Scroll)][Space(10)]
        [Toggle] _IsScroll("Is Scrolling", Float) = 0.0
        _SpeedX("Speed Axis X", float) = 0.0
        _SpeedY("Speed Axis Y", float) = 0.0
        
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
            Name "CustomDetail"
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
            #pragma shader_feature LIGHTMAP_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            sampler2D _BumpMap;
            TEXTURE2D(_EmissionMask);
            
            // 定数バッファー
            CBUFFER_START(UnityPerMaterial)
                half    _Cutoff;
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _EmissionMask_ST;
                half4 _EmissionColor;
                float _IsScroll;
                float _SpeedX;
                float _SpeedY;
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

                // Normal
                VertexNormalInputs normalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = normalInput.normalWS;
                OUT.tangentWS = normalInput.tangentWS;
                OUT.bitangentWS = normalInput.bitangentWS;
                
                // Light Map
                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS, OUT.vertexSH);
                
                float2 uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                // UV Scroll
                uv.x += frac(_IsScroll * _Time.y * _SpeedX);
                uv.y += frac(_IsScroll * _Time.y * _SpeedY);
                OUT.texcoord = uv;

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
                #ifdef _ALPHATEST_ON
                    _FinalColor.a = (_BaseTex.a - _Cutoff) / max(fwidth(_BaseTex.a), 0.0001) + 0.5;
                #endif
                
                #ifdef _NORMALMAP_ON
                    half3 _Normal = Map_Normal(_BumpMap, 1, IN.texcoord.xy, IN.normalWS, IN.tangentWS, IN.bitangentWS);
                #else
                    half3 _Normal = IN.normalWS;
                #endif
                half3 _LightColor = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, _Normal);
                
                #ifndef LIGHTMAP_ON
                float ld = dot(_MainLightPosition.xyz, _Normal);
                Light mainLight = GetMainLight();
                _LightColor.rgb += mainLight.color * mainLight.distanceAttenuation * max(0, ld);
                #endif
                
                _FinalColor.rgb = _FinalColor.rgb * _LightColor;

                #ifdef _EMISSION_ON
                const half _Emission = SAMPLE_TEXTURE2D(_EmissionMask, sampler_BaseMap, IN.texcoord).r;
                _FinalColor += _Emission * _EmissionColor;
                #endif
                
                _FinalColor.rgb = MixFog(_FinalColor.rgb, IN.FogFactor);
                
                return _FinalColor;
            }
            ENDHLSL
        }
        
//        Pass
//        {
//            Name "MotionVectors"
//            Tags
//            {
//                "RenderType" = "Opaque"
//                "LightMode" = "MotionVectors"
//            }
//
//            Cull [_CullMode]
//
//            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/OculusMotionVectorCore.hlsl"
//
//            #pragma vertex vertCutout
//            #pragma fragment fragCutout
//            
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local_fragment _ALPHATEST_ON
//
//            TEXTURE2D(_BaseMap);
//            SAMPLER(sampler_BaseMap);
//            
//            CBUFFER_START(UnityPerMaterial)
//                half    _Cutoff;
//                float4 _BaseMap_ST;
//                half4 _BaseColor;
//                float4 _EmissionMask_ST;
//                half4 _EmissionColor;
//                float _IsScroll;
//                float _SpeedX;
//                float _SpeedY;
//            CBUFFER_END
//            
//            struct AttributesCutout
//            {
//                float4 positionOS : POSITION;
//                float2 uv: TEXCOORD0;
//                float3 previousPositionOS : TEXCOORD4;
//                UNITY_VERTEX_INPUT_INSTANCE_ID
//            };
//
//            struct VaryingsCutout
//            {
//                float4 positionCS : SV_POSITION;
//                float4 uv: TEXCOORD0;
//                float4 curPositionCS : TEXCOORD8;
//                float4 prevPositionCS : TEXCOORD9;
//                UNITY_VERTEX_INPUT_INSTANCE_ID
//                UNITY_VERTEX_OUTPUT_STEREO 
//            };
//
//            VaryingsCutout vertCutout(AttributesCutout input)
//            {
//                VaryingsCutout output = (VaryingsCutout)0;
//                UNITY_SETUP_INSTANCE_ID(input);
//                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//
//                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
//                output.positionCS = vertexInput.positionCS;
//
//                // Cutoutのためuvを渡す
//                output.uv.xy = input.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
//                // UV Scroll（Timeでスクロール、UVは0~1なので小数点のみで加算）
//                output.uv.x += frac(_IsScroll * _Time.y * _SpeedX);
//                output.uv.y += frac(_IsScroll * _Time.y * _SpeedY);
//                
//                
//                float3 curWS = TransformObjectToWorld(input.positionOS.xyz);
//                output.curPositionCS = TransformWorldToHClip(curWS);
//                if (unity_MotionVectorsParams.y == 0.0)
//                {
//                    output.prevPositionCS = float4(0.0, 0.0, 0.0, 1.0);
//                }
//                else
//                {
//                    bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
//                    float3 effectivePositionOS = (hasDeformation ? input.previousPositionOS : input.positionOS.xyz);
//                    float3 previousWS = TransformPreviousObjectToWorld(effectivePositionOS);
//
//                    float4x4 previousOTW = GetPrevObjectToWorldMatrix();
//                    float4x4 currentOTW = GetObjectToWorldMatrix();
//                    if (!IsSmoothRotation(previousOTW._11_21_31, previousOTW._12_22_32, currentOTW._11_21_31, currentOTW._12_22_32))
//                    {
//                        output.prevPositionCS = output.curPositionCS;
//                    }
//                    else
//                    {
//                        output.prevPositionCS = TransformWorldToPrevHClip(previousWS);
//                    }
//                }
//                return output;
//            }
//            
//            half4 fragCutout(VaryingsCutout i) : SV_Target
//            {
//                float3 screenPos = i.curPositionCS.xyz / i.curPositionCS.w;
//                float3 screenPosPrev = i.prevPositionCS.xyz / i.prevPositionCS.w;
//                half4 color = (1);
//                
//                // Cutout
//                #ifdef _ALPHATEST_ON
//                    float alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv.xy).a;
//                    clip((alpha - _Cutoff) / max(fwidth(alpha), 0.0001) + 0.5);
//                #endif
//
//                color.xyz = screenPos - screenPosPrev;
//                return color;
//            }
//            ENDHLSL
//        }
        
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
    CustomEditor "CustomShaderGUI.DevelopmentDetailGUI"
}