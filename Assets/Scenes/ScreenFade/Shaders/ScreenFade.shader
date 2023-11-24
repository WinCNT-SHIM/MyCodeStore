Shader "App/ScreenFade"
{
    Properties
    {
        _Color("Color", Color) = (0,0,0,0)
    }
    
    SubShader  
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always
        
        Pass  
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END
            
            struct appdata_img
            {
                float4 vertex : POSITION;
                half2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f_img
            {
                float4 pos : SV_POSITION;
                half2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            v2f_img vert_img( appdata_img v )
            {
                v2f_img o = (v2f_img)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.texcoord;
                return o;
            }
            
            half4 frag (v2f_img i) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    } 
}