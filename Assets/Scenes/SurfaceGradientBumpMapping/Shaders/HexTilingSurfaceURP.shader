Shader "HexTiling/HexTilingSurfaceURP"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset]_BumpMap ("Bump Map", 2D) = "bump" {}
        _BumpIntensity("Bump Intensity", Range(0,1)) = 1
        [NoScaleOffset]_OcclusionMap ("Occlusion Map", 2D) = "white" {}
        
        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [NoScaleOffset]_MetallicGlossMap ("Metallic Gloss Map", 2D) = "black" {}
        
        _RotStrength("Rotate Strength", float) = 0
        _Contrast("Contrast", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            Name "HexTiling"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "HexTilingURP.hlsl"
            // #include "HexTiling.cginc"

            // Use shader model 3.0 target, to get nicer looking lighting
            #pragma target 3.0
            #pragma shader_feature LIGHTMAP_ON
            
            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _OcclusionMap;
            sampler2D _MetallicGlossMap;

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                float _BumpIntensity;
                float _RotStrength;
                float _Contrast;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float3 positionWS : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
            };

            void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
            {
                inputData = (InputData)0;

            #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
                inputData.positionWS = input.positionWS;
            #endif

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                
                float sgn = input.tangentWS.w;      // should be either +1 or -1
                float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
            
                // #if defined(_NORMALMAP)
                inputData.tangentToWorld = tangentToWorld;
                // #endif
                inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
            
                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = float4(0, 0, 0, 0);
                
            #if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
            #else
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
            #endif

                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.vertex);
                output.positionWS = TransformObjectToWorld(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                
                real sign = input.tangentOS.w * GetOddNegativeScale();
                half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
                output.tangentWS = tangentWS;

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 c;
                float3 weights;
                hex2colTex(c, weights, _MainTex, input.uv, _RotStrength, _Contrast);

                c *= _Color;

                float3 normal;
                bumphex2derivNMap(normal, weights, _BumpMap, input.uv, _RotStrength, _Contrast, _BumpIntensity);

                float4 occlusion;
                hex2colTex(occlusion, weights, _OcclusionMap, input.uv, _RotStrength, _Contrast);

                float4 roughness;
                hex2colTex(roughness, weights, _MetallicGlossMap, input.uv.xy, _RotStrength, _Contrast);
                
                // Surface Output
                SurfaceData surfaceData = (SurfaceData)0;
                
                surfaceData.albedo = c.rgb;
                surfaceData.normalTS = normal;
                surfaceData.smoothness = roughness.a * _Metallic;
                surfaceData.occlusion = occlusion.r;
                surfaceData.alpha = c.a;

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);
                // SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv, _BaseMap);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                // color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));
                color.a = 1.0;
                
                return color;
            }
            ENDHLSL
        }
    }
}
