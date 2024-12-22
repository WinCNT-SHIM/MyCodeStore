// Height Fog
sampler2D _HeightFogNoise;
float     _HeightFogNoisePower;
float     _HeightFogNoiseSpeedX;
float     _HeightFogNoiseSpeedY;
half4     _HeightFogColor;
float     _HeightFogDensity;
float     _MaxFogHeight;

// Method
float3 MixUniformHeightFog(
    float3 color,
    float3 heightFogcolor,
    float3 objectPos,
    float3 cameraPos,
    float heightFogDensity,
    float maxFogHeight,
    sampler2D noiseTex,
    float2 noiseUV,
    float noiseUVScrollX,
    float noiseUVScrollY,
    float noisePower)
{
	float distance1 = length(objectPos - cameraPos);
	color = color * pow((heightFogDensity), distance1);
	float lost = 1.0 - pow((heightFogDensity), distance1);
    
	color = color * (1.0 - lost);
	color += heightFogcolor * lost;
    
	return color;
    
    noiseUV.x += frac(_Time.y * noiseUVScrollX);
    noiseUV.y += frac(_Time.y * noiseUVScrollY);
    const float noise = tex2D(noiseTex, noiseUV).r * noisePower;
    
    // noiseで霧の適用カ所を調整する
    objectPos = objectPos + noise;
    float3 camToObj = cameraPos - objectPos;

    // 最初の計算式、備忘のため残す
    // float t;
    // if (objectPos.y < maxFogHeight) // 物が霧の中にある
    // {
    //     if (cameraPos.y > maxFogHeight) // カメラは霧の外にある
    //         t = (maxFogHeight - objectPos.y) / camToObj.y;
    //     else // カメラも霧の中にある
    //         t = 1.0;
    // }
    // else // 物が霧の外にいる
    // {
    //     if (cameraPos.y < maxFogHeight) // カメラは霧の中にいる
    //         t = (cameraPos.y - maxFogHeight) / camToObj.y;
    //     else // カメラも霧の外にいる
    //         t = 0.0;
    // }

    float t = 0.0;
    const float a = objectPos.y < maxFogHeight ? 0 : cameraPos.y;
    const float b = cameraPos.y > maxFogHeight ? maxFogHeight : -maxFogHeight;
    const float c = cameraPos.y > maxFogHeight ? -objectPos.y : 0;
    
    float d = a + b + c;
    // 物もカメラも霧の中にある
    d = max(objectPos.y, cameraPos.y) < maxFogHeight ? camToObj.y : d;
    // 物もカメラも霧の外にある
    d = min(objectPos.y, cameraPos.y) > maxFogHeight ? 0 : d;
    t = saturate(d / camToObj.y);
    
    const float distance = length(camToObj) * t;
    const float heightFogFactor = exp2(-heightFogDensity * distance * LOG2_E);
    color = lerp(heightFogcolor.rgb, color, heightFogFactor);
    return color;
}