Shader "SSS/SimpleSpriteAnimation"
{
    Properties
    {
        _Cutoff  ("Cutoff", Range(0.0, 1.0)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Texture)][Space(10)]
        [NoScaleOffset]_BaseMap("Sprite Sheet", 2D) = "white" {}
        [MainColor] _BaseColor("Tint Color", Color) = (1,1,1,1)
        
        [Header(Sprite Settings)][Space(10)]
        [Toggle] _Padding("UV Edge Padding", range(0.0, 1.0)) = 1.0
        _Column("Column", int) = 1
        _Row("Row", int) = 1
        _MaxFrameCount("Max Animation Frame Count", int) = 100
        
        [Header(Playing Settings)][Space(10)]
        [Toggle] _AutoLoop("Auto Loop", range(0.0, 1.0)) = 1.0
		_AnimationSpeed("Animation Speed", float) = 10.0
        
        _AnimationIndex("Animation Index to Play", int) = 0.0
        
        [HideInInspector]_Surface("__surface", Float) = 0.0
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("__SrcBlend", Float) = 1
        [HideInInspector][Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("__DstBlend", Float) = 0
        [HideInInspector] _ZWrite("__ZWrite", Float) = 1.0
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
            Name "Simple Sprite Animation"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
//            Blend SrcAlpha OneMinusSrcAlpha
//            ZWrite Off
//            Cull Off
            // https://forum.unity.com/threads/is-there-extra-performance-cost-for-blend-one-zero.1154021/
            Blend [_SrcBlend][_DstBlend]
            ZWrite [_ZWrite]
            Cull [_CullMode]
//            AlphaToMask [_AlphaToMask]
            AlphaToMask On

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
                uint _Row;
                uint _Column;
                uint _MaxFrameCount;
            // Playing Settings
                float _AutoLoop;
                half _AnimationSpeed;
                int _AnimationIndex;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                // セル1つのUV座標を計算
                const float2 ColRow = float2(_Column, _Row);
                const float2 maxCellTexelSize = float2(1.0, 1.0) / float2(_Column, _Row);
                float2 cell = input.uv / ColRow;
                cell = cell * (maxCellTexelSize - _BaseMap_TexelSize.xy * _Padding) / maxCellTexelSize;
                
                // 再生するインデクスの計算
                uint index = max(0, (_AnimationIndex - 1));
                // Animation IndexとAutoLoopフラグが同時に入力された場合はAnimation Indexを優先する
                const float autoLoop = _AnimationIndex >= 1 ? 0.0 : _AutoLoop;
                index += (autoLoop * _Time.y * _AnimationSpeed);
                // Animationの最大Frame数が制限されているそちらを使用する 
                index = index % min((_Column * _Row), _MaxFrameCount);
                
                uint columnIndex = index % _Column;
                uint rowIndex = _Row - (index /_Column) - 1;
                
                // UV Coord Adjustment
                const float2 uvOffset = (float2(columnIndex, rowIndex) / ColRow) + (_BaseMap_TexelSize.xy * (0.5 * _Padding));
                output.uv = cell + uvOffset;
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 outColor = half4(0.0, 0.0, 0.0, 1.0);
                
                const half4 albedoColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                
                outColor.rgb = albedoColor.rgb * _BaseColor.rgb;
                // #ifdef _ALPHATEST_ON
                    outColor.a = (albedoColor.a - _Cutoff) / max(fwidth(albedoColor.a), 0.0001) + 0.5;
                // #endif
                
                return outColor;
            }
            ENDHLSL
        }
    }
}