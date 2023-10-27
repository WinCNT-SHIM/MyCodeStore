Shader "SSS/SimpleSpriteAnimation"
{
    Properties
    {
        _Cutoff  ("Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Sprite Settings)][Space(10)]
        [Toggle] _Padding("Use Edge Padding", range(0.0, 1.0)) = 0.0
        [NoScaleOffset]_BaseMap("Sprite Sheet", 2D) = "white" {}
        [MainColor] _BaseColor("Tint Color", Color) = (1,1,1,1)
        _Column("Column", int) = 1
        _Row("Row", int) = 1
        _MaxFrameCount("Max Animation Frame Count", int) = 100
        
        [Header(Animation Settings)][Space(10)]
        [Toggle] _AutoLoop("Auto Loop", range(0.0, 1.0)) = 1.0
        _AnimationSpeed("Animation Speed", float) = 10.0
        _AnimationIndex("Animation Index Control", int) = 0.0
        
        // Editmode Properties
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("__SrcBlend", Float) = 1
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("__DstBlend", Float) = 0
        [HideInInspector][Enum(Off, 0, On, 1)] _ZWrite("__ZWrite", Float) = 1.0
        [HideInInspector][Enum(Off, 0, On, 1)] _AlphaToMask("__AlphaToMask", Float) = 0.0
        [HideInInspector] _QueueOffset("Queue offset", Range(-50.0, 50.0)) = 0.0
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
            Name "Simple Sprite Animation"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            // https://forum.unity.com/threads/is-there-extra-performance-cost-for-blend-one-zero.1154021/
            Blend [_SrcBlend][_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]
            AlphaToMask [_AlphaToMask]

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            
            
            // 変数
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            
            // 定数バッファー
            CBUFFER_START(UnityPerMaterial)
                half    _Cutoff;
                float4 _BaseMap_ST;
                float4 _BaseMap_TexelSize;
                half4 _BaseColor;
            // Sprite Settings
                float _Padding;
                int _Row;
                int _Column;
                int _MaxFrameCount;
            // Playing Settings
                float _AutoLoop;
                half _AnimationSpeed;
                int _AnimationIndex;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                ZERO_INITIALIZE(Varyings, output);
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                int column = max(_Column, 1);
                int row = max(_Row, 1);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // セル1つのUV座標を計算
                const float2 ColRow = float2(column, row);
                const float2 maxCellTexelSize = float2(1.0, 1.0) / float2(column, row);
                float2 cell = input.uv / ColRow;
                cell = cell * (maxCellTexelSize - _BaseMap_TexelSize.xy * _Padding) / maxCellTexelSize;
                
                // 再生するインデクスの計算
                uint index = max(0, (_AnimationIndex - 1));
                // Animation IndexとAutoLoopフラグが同時に入力された場合はAnimation Indexを優先する
                const float autoLoop = _AnimationIndex >= 1 ? 0.0 : _AutoLoop;
                index += (autoLoop * _Time.y * _AnimationSpeed);
                // Animationの最大Frame数が制限されているそちらを使用する 
                index = index % min((column * row), max(_MaxFrameCount, 1));
                
                uint columnIndex = index % column;
                uint rowIndex = row - (index /column) - 1;
                
                // UV Coord Adjustment
                const float2 uvOffset = (float2(columnIndex, rowIndex) / ColRow) + (_BaseMap_TexelSize.xy * (0.5 * _Padding));
                output.uv = cell + uvOffset;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                half4 outColor = half4(0.0, 0.0, 0.0, 1.0);
                const half4 albedoColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                outColor.rgb = albedoColor.rgb * _BaseColor.rgb;

                half alpha = 1.0f;
            #ifdef _ALPHATEST_ON
                alpha  = saturate((albedoColor.a - _Cutoff) / max(fwidth(albedoColor.a), 0.0001) + 0.5);
            #endif
            #ifdef _SURFACE_TYPE_TRANSPARENT
                alpha *= _BaseColor.a;
            #endif
                outColor.a =  alpha;
                
                return outColor;
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
//            #pragma fragment frag
//            
//            // -------------------------------------
//            // Material Keywords
//            #pragma shader_feature_local_fragment _ALPHATEST_ON
//            #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
//
//            // 変数
//            TEXTURE2D(_BaseMap);
//            SAMPLER(sampler_BaseMap);
//            
//            // 定数バッファー
//            CBUFFER_START(UnityPerMaterial)
//                half    _Cutoff;
//                float4 _BaseMap_ST;
//                float4 _BaseMap_TexelSize;
//                half4 _BaseColor;
//            // Sprite Settings
//                float _Padding;
//                int _Row;
//                int _Column;
//                int _MaxFrameCount;
//            // Playing Settings
//                float _AutoLoop;
//                half _AnimationSpeed;
//                int _AnimationIndex;
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
//                float2 uv: TEXCOORD0;
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
//                // UV Coord Adjustment
//                int column = max(_Column, 1);
//                int row = max(_Row, 1);
//                const float2 ColRow = float2(column, row);
//                const float2 maxCellTexelSize = float2(1.0, 1.0) / float2(column, row);
//                float2 cell = input.uv / ColRow;
//                cell = cell * (maxCellTexelSize - _BaseMap_TexelSize.xy * _Padding) / maxCellTexelSize;
//                
//                uint index = max(0, (_AnimationIndex - 1));
//                const float autoLoop = _AnimationIndex >= 1 ? 0.0 : _AutoLoop;
//                index += (autoLoop * _Time.y * _AnimationSpeed);
//                index = index % min((column * row), max(_MaxFrameCount, 1));
//                
//                uint columnIndex = index % column;
//                uint rowIndex = row - (index /column) - 1;
//                const float2 uvOffset = (float2(columnIndex, rowIndex) / ColRow) + (_BaseMap_TexelSize.xy * (0.5 * _Padding));
//                output.uv = cell + uvOffset;
//
//                // Motion Vector
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
//            ENDHLSL
//        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "CustomShaderGUI.SimpleSpriteAnimationGUI"
}