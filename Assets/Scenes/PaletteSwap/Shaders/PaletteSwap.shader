Shader "SSS/PaletteSwap"
{
    Properties
    {
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 2 // Back
        
        [Header(Texture)][Space(10)]
        _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        
        [Header(Palette Swap)]
        [Toggle] _PaletteSwap("Palette Swap On/Off", Float) = 0.0
        [NoScaleOffset] _PaletteSwapMask ("Palette Swap Mask", 2D) = "black" { }
        _PaletteSwapMask1Color ("Palette Swap Mask 1 Color", Color) = (0, 0, 0, 1)
        _PaletteSwapMask2Color ("Palette Swap Mask 2 Color", Color) = (0, 0, 0, 1)
        _PaletteSwapMask3Color ("Palette Swap Mask 3 Color", Color) = (0, 0, 0, 1)
        [Enum(CustomShaderGUI.PaletteSwapMode)] _PaletteSwapMask1ColorMode ("Palette Swap Mask 1 Color Mode", Float) = 0
        [Enum(CustomShaderGUI.PaletteSwapMode)] _PaletteSwapMask2ColorMode ("Palette Swap Mask 2 Color Mode", Float) = 0
        [Enum(CustomShaderGUI.PaletteSwapMode)] _PaletteSwapMask3ColorMode ("Palette Swap Mask 3 Color Mode", Float) = 0
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
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_PaletteSwapMask);
            SAMPLER(sampler_PaletteSwapMask);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;

                half4 _PaletteSwapMask1Color;
                half4 _PaletteSwapMask2Color;
                half4 _PaletteSwapMask3Color;
                float _PaletteSwapMask1ColorMode;
                float _PaletteSwapMask2ColorMode;
                float _PaletteSwapMask3ColorMode;
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

            float3 GetPaletteSwapColor(float3 baseColor, half3 maskColor, float mask, float paletteSwapMode)
            {
                // Gray Scale
                const float3 color = 0.299 * baseColor.r + 0.587 * baseColor.g + 0.114 * baseColor.b;
                
                // Linear
                float3 colorLinearMode = color + mask * maskColor;
                // Multiply
                float3 colorMultiplyMode = color * mask * maskColor;
                // Screen
                float3 colorScreenMode = colorLinearMode - colorMultiplyMode;
                // Overlay
                float3 s = step(0.5, color);
                float3 colorOverlayMode = 2 * lerp(colorMultiplyMode, colorScreenMode, s) - s;

                // The selected mode is multiplied by 1, while the others are multiplied by 0, resulting in only the color of the selected mode remaining.
                const int mode = int(paletteSwapMode);
                colorLinearMode   *= (mode & 1);
                colorMultiplyMode *= (mode & 2) >> 1;
                colorScreenMode   *= (mode & 4) >> 2;
                colorOverlayMode  *= (mode & 8) >> 3;

                // Adjust the range reflected by the mask.
                return saturate(lerp(baseColor, (colorLinearMode + colorMultiplyMode + colorScreenMode + colorOverlayMode), mask));
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
                half4 _BaseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.texcoord);

                // Palette Swap
                half3 paletteSwapMask = SAMPLE_TEXTURE2D(_PaletteSwapMask, sampler_PaletteSwapMask, IN.texcoord).rgb;
                _BaseTex.rgb = GetPaletteSwapColor(_BaseTex, _PaletteSwapMask1Color.rgb, paletteSwapMask.r, _PaletteSwapMask1ColorMode);
                _BaseTex.rgb = GetPaletteSwapColor(_BaseTex, _PaletteSwapMask2Color.rgb, paletteSwapMask.g, _PaletteSwapMask2ColorMode);
                _BaseTex.rgb = GetPaletteSwapColor(_BaseTex, _PaletteSwapMask3Color.rgb, paletteSwapMask.b, _PaletteSwapMask3ColorMode);
                
                _FinalColor = _BaseTex * _BaseColor;
                
                return _FinalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "CustomShaderGUI.PaletteSwapGUI"
}