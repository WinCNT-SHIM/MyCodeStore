Shader "SSS/HSVShift"
{
    Properties
    {
        [KeywordEnum(TEST1, TEST2)] _SAMPLE("Sample HSV", Float) = 0.0
        [KeywordEnum(Linear, Multiply, Screen, Overlay)] _Mode("Sample Blend Mode", Float) = 0.0
        
        [MainTexture] _BaseMap ("Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Temp("Color Picker", Color) = (1, 1, 1, 1)
        
        [Header(HSB)]
        _Hue("Hue", Range(-0.5, 0.5)) = 0.0
        _Saturation("Saturation", Range(-1.0, 1.0)) = 0.0
        _Brightness("Brightness", Range(-1.0, 1.0)) = 0.0
        
        [Header(Blend Mode)]
        _BlendColor("Blend Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma multi_compile _SAMPLE_TEST1 _SAMPLE_TEST2
            #pragma multi_compile _Mode_LINEAR _Mode_MULTIPLY _Mode_SCREEN _Mode_OVERLAY
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Hue;
            float _Saturation;
            float _Brightness;
            CBUFFER_END

            float3 RGBtoHSV(float3 color)
            {
                float3 c = color;
                
                // RGB -> HSV(HSB)
                const float brightness = saturate(max(c.r, max(c.g, c.b))); // Brightness == Value
                const float small = saturate(min(c.r, min(c.g, c.b)));
                const float chrome = saturate(brightness - small);
                
                float hue = 0.0;
                float chromeReverse = chrome > 0.0 ? 1.0 / chrome : 0.0;

                float isRedBrightest   = saturate(step(brightness, c.r) * step(c.r, brightness));
                float isGreenBrightest = saturate(step(brightness, c.g) * step(c.g, brightness));
                isGreenBrightest = isRedBrightest > 0.5f ? 0.0f : isGreenBrightest;
                float isBlueBrightest  = isRedBrightest + isGreenBrightest > 0.5f ? 0.0f : 1.0f;

                hue += isRedBrightest   * ((c.g - c.b) * chromeReverse + 6.0) % 6.0;
                hue += isGreenBrightest * ((c.b - c.r) * chromeReverse + 2.0);
                hue += isBlueBrightest  * ((c.r - c.g) * chromeReverse + 4.0);
                hue *= 60;
                
                float saturation = 0.0;
                saturation = brightness > 0.0 ? chrome / brightness : 0.0;
                
                return float3(hue, saturation, brightness);
            }

            float3 HSVtoRGB(float3 hsv)
            {
                float hue = hsv.x;
                float saturation = hsv.y;
                float brightness = hsv.z; // Brightness == Value
                
                // HSV(HSB) -> RGB
                float4 color = float4(0, 0, 0, 1);
                
                const float rk = (hue / 60 + 5) % 6;
                const float gk = (hue / 60 + 3) % 6;
                const float bk = (hue / 60 + 1) % 6;
                color.r = brightness - brightness * saturation * max(0, min(rk, min(4 - rk, 1)));
                color.g = brightness - brightness * saturation * max(0, min(gk, min(4 - gk, 1)));
                color.b = brightness - brightness * saturation * max(0, min(bk, min(4 - bk, 1)));
                
                return color.rgb;
            }

            // hsbShift.x == hue
            // hsbShift.y == saturation
            // hsbShift.z == brightness
            float4 ApplyHSBShift(float4 baseColor, float3 hsbShift, float mask)
            {
                float3 c = baseColor.rgb;
                
            #if UNITY_COLORSPACE_GAMMA
                c = Gamma22ToLinear(baseColor);
            #endif

                float3 hsv = RGBtoHSV(c);
                float hue = hsv.x;
                float saturation = hsv.y;
                float brightness = hsv.z;

                // Hue Shift
                hue += hsbShift.x * 360;
                // Saturation Shift
                saturation = saturate(saturation * (hsbShift.y + 1.0));

                // HSV -> RGB
                float4 finalColor = float4(0, 0, 0, baseColor.a);
                finalColor.rgb = HSVtoRGB(float3(hue, saturation, brightness));

                // Brightness Shift
                if (hsbShift.z > 0.0)
                    finalColor.rgb = saturate(finalColor.rgb + Gamma22ToLinear(hsbShift.z));
                else
                    finalColor.rgb = saturate(finalColor.rgb * Gamma22ToLinear(hsbShift.z + 1.0));

                finalColor.rgb = saturate(finalColor.rgb);
                
            #ifdef UNITY_COLORSPACE_GAMMA
                finalColor = LinearToGamma22(finalColor);
            #endif
                
                // Mask
                finalColor.rgb = lerp(baseColor.rgb, finalColor.rgb, mask);
                return finalColor;
            }
            
            float3 RGBtoHSL(float3 color)
            {
                float3 c = color;
                
                // RGB -> HSV(HSB)
                const float brightness = saturate(max(c.r, max(c.g, c.b))); // Brightness == Value
                const float small = saturate(min(c.r, min(c.g, c.b)));
                const float chrome = saturate(brightness - small);

                const float lightness = (brightness + small) * 0.5;
                
                float hue = 0;
                float chromeReverse = chrome > 0.0 ? 1.0 / chrome : 0.0;

                float isRedBrightest   = saturate(step(brightness, c.r) * step(c.r, brightness));
                float isGreenBrightest = saturate(step(brightness, c.g) * step(c.g, brightness));
                isGreenBrightest = isRedBrightest > 0.5f ? 0.0f : isGreenBrightest;
                float isBlueBrightest  = isRedBrightest + isGreenBrightest > 0.5f ? 0.0f : 1.0f;

                hue += (isRedBrightest   * ((c.g - c.b) * chromeReverse + 6) % 6);
                hue += (isGreenBrightest * ((c.b - c.r) * chromeReverse + 2));
                hue += (isBlueBrightest  * ((c.r - c.g) * chromeReverse + 4));
                hue *= 60;
                
                float saturation = 0.0;
                // saturation = brightness > 0.0 ? chrome / brightness : 0.0;
                saturation = lightness > 0.0 && lightness < 1.0 ? chrome / (1.0 - abs(2.0 * lightness - 1.0)) : 0.0;
                
                return float3(hue, saturation, lightness);
            }
            
            float3 HSLtoRGB(float3 hsl)
            {
                float hue = hsl.x;
                float saturation = hsl.y;
                float lightness = hsl.z;
                
                // HSV(HSB) -> RGB
                float4 color = float4(0, 0, 0, 1);

                const float alpha = saturation * min(lightness, 1.0 - lightness);

                float kRed = (hue / 30.0) % 12;
                float kGreen = (hue / 30.0 + 8) % 12;
                float kBlue = (hue / 30.0 + 4) % 12;

                color.r = lightness - alpha * max(-1, min(min(kRed   - 3, 9 - kRed),   1));
                color.g = lightness - alpha * max(-1, min(min(kGreen - 3, 9 - kGreen), 1));
                color.b = lightness - alpha * max(-1, min(min(kBlue  - 3, 9 - kBlue),  1));
                color.rgb = saturate(color.rgb);
                
                return color.rgb;
            }
            
            float4 ApplyHSLShift(float4 baseColor, float3 hslShift, float mask)
            {
                float3 c = baseColor.rgb;
                
            #if UNITY_COLORSPACE_GAMMA
                c = Gamma22ToLinear(baseColor);
            #endif

                float3 hsl = RGBtoHSL(c);
                // return float4(hsl/360, 1.0);
                float hue = hsl.x;
                float saturation = hsl.y;
                float lightness = hsl.z;

                // Hue Shift
                hue += hslShift.x * 360.0;
                if (hue < 0.0)
                    hue += 360.0;
                else if (hue > 360.0)
                    hue -= 360.0;
                
                // Saturation Shift
                if (hslShift.y > 0.0)
                    saturation = (saturation + (hslShift.y));
                else
                    saturation = (saturation * (hslShift.y + 1.0));

                // HSV -> RGB
                float4 finalColor = float4(0, 0, 0, baseColor.a);
                finalColor.rgb = HSLtoRGB(float3(hue, saturation, lightness));

                // Lightness Shift
                if (hslShift.z > 0.0)
                    finalColor.rgb = saturate(finalColor.rgb + Gamma22ToLinear(hslShift.z));
                else
                    finalColor.rgb = saturate(finalColor.rgb * Gamma22ToLinear(hslShift.z + 1.0));

                finalColor.rgb = saturate(finalColor.rgb);
                
            #ifdef UNITY_COLORSPACE_GAMMA
                finalColor = LinearToGamma22(finalColor);
            #endif
                
                // Mask
                finalColor.rgb = lerp(baseColor.rgb, finalColor.rgb, mask);
                return finalColor;
            }
            
            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(v.vertex);
                o.vertex = vertexInput.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap , i.uv);
                baseColor = baseColor * _BaseColor;
                
                float3 hsbShift = float3(_Hue, _Saturation, _Brightness);
                baseColor = ApplyHSBShift(baseColor, hsbShift, 1);
                
                return baseColor;
            }
            ENDHLSL
        }
    }
}
