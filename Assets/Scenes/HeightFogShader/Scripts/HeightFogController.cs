using UnityEngine;
using UnityEngine.Rendering;

// [ExecuteAlways]
[ExecuteInEditMode]
public class HeightFogController : MonoBehaviour
{
    // Properties
    [Header("Height Fog")]
    [SerializeField] private bool HeightFogOn = false;
    [ColorUsage(true, true)] public Color HeightFogColor = Color.gray;
    [Range(0.0f, 1.0f)]public float HeightFogDensity = 0.0f;
    public float MaxFogHeight = 1.0f;
    
    [Header("Height Fog Noise")]
    public Texture HeightFogNoise = null;
    [Range(0.0f, 5.0f)]public float NoisePower = 1.0f;
    [Range(-1.0f, 1.0f)]public float NoiseScrollSpeedX = 0.0f;
    [Range(-1.0f, 1.0f)]public float NoiseScrollSpeedY = 0.0f;

    // Property ID
    private int _HeightFogNoise = -1;
    private int _HeightFogNoisePower = -1;
    private int _HeightFogNoiseSpeedX = -1;
    private int _HeightFogNoiseSpeedY = -1;
    private int _HeightFogColor = -1;
    private int _HeightFogDensity = -1;
    private int _MaxFogHeight = -1;
    
    // Global Keyword
    private GlobalKeyword HeightFogKeyword;
    
    void Awake()
    {
        _HeightFogNoise = Shader.PropertyToID("_HeightFogNoise");
        _HeightFogNoisePower = Shader.PropertyToID("_HeightFogNoisePower");
        _HeightFogNoiseSpeedX = Shader.PropertyToID("_HeightFogNoiseSpeedX");
        _HeightFogNoiseSpeedY = Shader.PropertyToID("_HeightFogNoiseSpeedY");
        _HeightFogColor = Shader.PropertyToID("_HeightFogColor");
        _HeightFogDensity = Shader.PropertyToID("_HeightFogDensity");
        _MaxFogHeight = Shader.PropertyToID("_MaxFogHeight");
        
        HeightFogKeyword = GlobalKeyword.Create("_HEIGHT_FOG");
    }

    void Update()
    {
        if (HeightFogOn)
            Shader.EnableKeyword(HeightFogKeyword);
        else
            Shader.DisableKeyword(HeightFogKeyword);
        
        Shader.SetGlobalColor(_HeightFogColor, HeightFogColor);
        Shader.SetGlobalFloat(_HeightFogDensity, (float)HeightFogDensity);
        Shader.SetGlobalFloat(_MaxFogHeight, MaxFogHeight);
        
        Shader.SetGlobalTexture(_HeightFogNoise, HeightFogNoise);
        Shader.SetGlobalFloat(_HeightFogNoisePower, NoisePower);
        Shader.SetGlobalFloat(_HeightFogNoiseSpeedX, NoiseScrollSpeedX);
        Shader.SetGlobalFloat(_HeightFogNoiseSpeedY, NoiseScrollSpeedY);
    }

    void OnDisable()
    {
        Shader.DisableKeyword(HeightFogKeyword);
    }

    void OnDestroy()
    {
        Shader.DisableKeyword(HeightFogKeyword);
    }
}
