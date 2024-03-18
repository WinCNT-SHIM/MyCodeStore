Shader "App/Vignette"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendSrc ("Blend Source", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)]_BlendDst ("Blend Destination", Float) = 0
        _ZWrite ("Z Write", Float) = 0
    }
    SubShader
    {
        Tags { "IgnoreProjector" = "True" }

        Pass
        {
            Blend [_BlendSrc] [_BlendDst]
            ZTest Always
            ZWrite [_ZWrite]
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ QUADRATIC_FALLOFF
            // #pragma enable_d3d11_debug_symbols

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ScaleAndOffset0[2];
            float4 _ScaleAndOffset1[2];

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                ZERO_INITIALIZE(v2f, o);
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // _ScaleAndOffset0はVertexを外側に、_ScaleAndOffset1は内側に移動するための値が格納されている
                // また、VignetteのMesh作成時、奇数のVertextのUVのXは0、偶数のVertexのUVのXは1になるようにしているため、
                // Lerpで、奇数のVertextは外側、偶数のVertexは内側に移動される
                float4 scaleAndOffset = lerp(_ScaleAndOffset0[unity_StereoEyeIndex], _ScaleAndOffset1[unity_StereoEyeIndex], v.uv.x);

                // UNITY_NEAR_CLIP_VALUEはHClip Spaceで、Vignetteをカメラの手前に表示するための値
                o.vertex = float4(scaleAndOffset.zw + v.vertex.xy * scaleAndOffset.xy, UNITY_NEAR_CLIP_VALUE, 1);

                o.color.rgb = _Color.rgb;
                // UVのYをアルファ値として使う
                // VignetteのMesh作成時に不透明はYを1、半透明は外->内が0->1になるようにしている
                o.color.a = v.uv.y;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
#if QUADRATIC_FALLOFF
                i.color.a *= i.color.a;
#endif
                return i.color;
            }
            ENDHLSL
        }
    }
}
