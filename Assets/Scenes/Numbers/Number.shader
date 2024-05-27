Shader "App/UI/Number"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Number ("Number", Int) = 3
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct InstanceData
            {
                float3 position;
                float keta;
                uint number;
                float endTime;
                float hue;
            };

            uniform StructuredBuffer<InstanceData> instancingBuffer;

            struct appdata
            {
                float4 vertex : POSITION;
                uint vid : SV_VertexID;
                uint instanceId : SV_InstanceID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float hue : TEXCOORD0;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                #if UNITY_ANY_INSTANCING_ENABLED
                    const uint iid = unity_InstanceID;
                #else
                    const uint iid = v.instanceId;
                #endif

                InstanceData data = instancingBuffer[iid];
                float3 offset = data.position;
                float sizeM = abs(mul(UNITY_MATRIX_MV, float4(data.position, 1.0)).z);
                sizeM /= 10;
                sizeM = min(0.3, sizeM);
                float number = data.number;
                float duration = saturate(data.endTime - _Time.g);
                float size = min(1, sin(pow(duration, 0.2)) * 1.3) * sizeM;
                float keta = data.keta;
                float3 tex = tex2Dlod(_MainTex, float4((v.vid + 0.5) * _MainTex_TexelSize.x,
                                                             (number + 0.5) * _MainTex_TexelSize.y, 0, 0)).rgb;

                float3 posOS = -float3(tex.x - 0.5, tex.z - 0.5, tex.y - 0.5) * size;
                posOS.x -= keta * 0.5 * size;
                offset.y += sin((1 - duration) * 1.3 * PI / 2) * 3 * sizeM;

                float3 yup = float3(0.0, 1.0, 0.0);
                float3 up = mul((float3x3)unity_ObjectToWorld, yup);
                float3 cameraDirection = _WorldSpaceCameraPos - offset;
                float3 right = normalize(cross(cameraDirection, up));
                float3 forward = normalize(cross(up, right));
                float4x4 billboard_matrix = {
                    right.x, up.x, forward.x, offset.x,
                    right.y, up.y, forward.y, offset.y,
                    right.z, up.z, forward.z, offset.z,
                    0, 0, 0, 1,
                };

                o.vertex = mul(UNITY_MATRIX_VP, mul(billboard_matrix, float4(posOS, 1)));
                o.hue = data.hue;

                return o;
            }

            
            float3 hsv(float h, float s, float v){
                float4 t = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(float3(h, h, h) + t.xyz) * 6.0 - float3(t.w, t.w, t.w));
                return v * lerp(float3(t.x, t.x, t.x), clamp(p - float3(t.x, t.x, t.x), 0.0, 1.0), s);
            }
            
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                
                return float4(hsv(i.hue, 1, 1), 1);
            }
            ENDHLSL
        }
    }
}