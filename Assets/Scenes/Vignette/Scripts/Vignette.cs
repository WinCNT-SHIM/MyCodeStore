using UnityEngine;
using UnityEngine.Rendering;

public class Vignette : MonoBehaviour
{
    [SerializeField, Tooltip("表示に掛かる時間")] private float _showIntervalTime = 0.125f;
    [SerializeField, Tooltip("非表示に掛かる時間")] private float _hideIntervalTime = 0.25f;
    
    [SerializeField, Tooltip("Vignette用Meshの形状（基本Normalで問題ありません）")]
    private MeshComplexityLevel meshComplexity = MeshComplexityLevel.Normal;
    [SerializeField, Tooltip("VignetteのFalloffの設定")]
    private FalloffType falloff = FalloffType.Linear;
    [Tooltip("Vignetteの垂直FOV")]
    public float vignetteFieldOfView = 60;
    [Tooltip("Vignetteの水平FOVを調整するAspect Ratio")]
    public float vignetteAspectRatio = 1f;
    [Tooltip("VignetteのFalloffの広さ")]
    public float vignetteFalloffDegrees = 10f;
    [ColorUsage(false)] public Color vignetteColor;
    [Tooltip("半透明と不透明のVignetteの間隔")]
    public float middleOffset = 1.02f;

    private float _rate = 0f;
    private float _targetRate = 0f;
    private bool _isAnimating = false;
    private bool _isVignetteStart = true;
    
    /// <summary>
    /// Vignette用Meshの形状
    /// </summary>
    private enum MeshComplexityLevel
    {
        VerySimple,
        Simple,
        Normal,
        Detailed,
        VeryDetailed
    }

    /// <summary>
    /// VignetteのFalloffの設定
    /// </summary>
    private enum FalloffType
    {
        Linear,
        Quadratic
    }
    private static readonly string QUADRATIC_FALLOFF = "QUADRATIC_FALLOFF";

    [SerializeField][HideInInspector] private Shader vignetteShader;
    
    private Camera _camera;
    private MeshFilter _opaqueMeshFilter;
    private MeshFilter _transparentMeshFilter;
    private MeshRenderer _opaqueMeshRenderer;
    private MeshRenderer _transparentMeshRenderer;

    private Mesh _opaqueMesh;
    private Mesh _transparentMesh;
    private Material _opaqueMaterial;
    private Material _transparentMaterial;
    private int _shaderScaleAndOffset0Property;
    private int _shaderScaleAndOffset1Property;
    
    private readonly float[] _innerScaleX = new float[2];
    private readonly float[] _innerScaleY = new float[2];
    private readonly float[] _middleScaleX = new float[2];
    private readonly float[] _middleScaleY = new float[2];
    private readonly float[] _outerScaleX = new float[2];
    private readonly float[] _outerScaleY = new float[2];
    private readonly float[] _offsetX = new float[2];
    private readonly float[] _offsetY = new float[2];
    private readonly float[] _maxVignetteRange = new float[2];
    
    private readonly Vector4[] _TransparentScaleAndOffset0 = new Vector4[2];
    private readonly Vector4[] _TransparentScaleAndOffset1 = new Vector4[2];
    private readonly Vector4[] _OpaqueScaleAndOffset0 = new Vector4[2];
    private readonly Vector4[] _OpaqueScaleAndOffset1 = new Vector4[2];
    
    private bool _opaqueVignetteVisible = false;
    private bool _transparentVignetteVisible = false;
    
#if UNITY_EDITOR
    // in the editor, allow these to be changed at runtime
    private MeshComplexityLevel _InitialMeshComplexity;
    private FalloffType _InitialFalloff;
#endif
    private int GetTriangleCount()
    {
        switch (meshComplexity)
        {
            case MeshComplexityLevel.VerySimple: return 32;
            case MeshComplexityLevel.Simple: return 64;
            case MeshComplexityLevel.Normal: return 128;
            case MeshComplexityLevel.Detailed: return 256;
            case MeshComplexityLevel.VeryDetailed: return 512;
            default: return 128;
        }
    }

    /// <summary>
    /// Vignette用不透明Mesh、半透明Meshを作成する
    /// </summary>
    private void BuildMeshes()
    {
#if UNITY_EDITOR
        _InitialMeshComplexity = meshComplexity;
#endif
        // Vignette用Meshの構成する三角形の数
        // ※ 四角いVignetteのためには8個のVertexが必要
        int triangleCount = GetTriangleCount();

        // Vignette用半透明MeshのVertexバッファー、及びUVの情報を格納
        Vector3[] innerVerts = new Vector3[triangleCount];
        Vector2[] innerUVs = new Vector2[triangleCount];
        // Vignette用不透明MeshのVertexバッファー、及びUVの情報を格納
        Vector3[] outerVerts = new Vector3[triangleCount];
        Vector2[] outerUVs = new Vector2[triangleCount];
        // Vignette用半透明Meshと不透明MeshのIndexバッファー(VertexのTriangleの順番)を格納
        int[] tris = new int[triangleCount * 3];
        
        // Vignette用Meshの作成
        // この処理だけではVertexが三角形ではなくて線になるが、
        // Shader側でUVのXの値からOuterかInnerかを判定し、移動させることで三角形になる
        for (int i = 0; i < triangleCount; i += 2)
        {
            // Vertexの座標が円周上になるように計算
            float angle = 2 * i * Mathf.PI / triangleCount;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            // 不透明MeshのVertex、UVの情報を格納
            // outerVerts[i]とouterVerts[i + 1]は同じ座標
            outerVerts[i] = new Vector3(x, y, 0);
            outerVerts[i + 1] = new Vector3(x, y, 0);
            // UVのXはVertex ShaderでVertexを外側・内側に移動するかを判定するために使われる
            // 結果的にouterVerts[i]は外側、outerVerts[i + 1]は内側に移動される
            // UVのYはVignetteのMeshの半透明度(アルファ)に使われるので、不透明には1に設定
            outerUVs[i] = new Vector2(0, 1);
            outerUVs[i + 1] = new Vector2(1, 1);

            // 半透明MeshのVertex、UVの情報を格納
            // innerVerts[i]とinnerVerts[i + 1]は同じ座標
            innerVerts[i] = new Vector3(x, y, 0);
            innerVerts[i + 1] = new Vector3(x, y, 0);
            // UVのXはVertex ShaderでVertexを外側・内側に移動するかを判定するために使われる
            // 結果的にinnerVerts[i]は内側、innerVerts[i + 1]は外側に移動される
            // UVのYはVignetteのMeshの半透明度(アルファ)のために使われる
            // 外側->内側が1->0なので、結果的に半透明のVignetteがグラデーションになる
            innerUVs[i] = new Vector2(0, 1);
            innerUVs[i + 1] = new Vector2(1, 0);

            // 三角形のIndexを計算
            int ti = i * 3;
            tris[ti] = i;
            tris[ti + 1] = i + 1;
            tris[ti + 2] = (i + 2) % triangleCount;
            tris[ti + 3] = i + 1;
            tris[ti + 4] = (i + 3) % triangleCount;
            tris[ti + 5] = (i + 2) % triangleCount;
            
            // 例えば、triangleCountが8の場合、Meshの形状は以下となる（不透明や半透明も同じ形状）
            //                      , - ~V23~ - ,
            //                  , '    I     I    ' ,
            //                ,     I           I     ,
            //               ,   I                 I   ,
            //              , I                       I ,
            //              V45                         V01
            //              , I                       I ,
            //               ,   I                 I   ,
            //                ,     I           I     ,
            //                  ,      I     I     , '
            //                    ' - , _V67_ ,  '
            // （VはVertexとその番号で、IはIndex順に引いた線のこと）
            //
            // 同じ座標にあるVertexはVertex Shaderにて移動されて三角形になる
            // UVのXの値からVertexを外側に移動するか、内側に移動するかを判定する
            // 移動の値はCalculateVignetteScaleAndOffsetで計算してShaderに渡す
            // 
            // 例えば、triangleCountが8の場合のMeshの形状は以下となる
            //                            V2
            //                         I  I  I
            //                     I    I I I    I
            //                  I   ,  I~ I ~I  ,   I
            //               I  , '   I   I   I   ' ,  I
            //            I   ,      I    V3    I     ,   I 
            //         I     ,      I   I    I   I     ,     I
            //      I       ,      I  I        I  I     ,       I
            //    V4 I I I I I I I V5            V1 I I I I I I I V0
            //      I       ,      I  I        I  I     ,       I
            //         I     ,      I   I    I   I     ,     I
            //            I   ,      I    V7    I     ,   I
            //               I  ,     I   I   I    , '  I
            //                  I ' - ,I_ I _I,  '   I
            //                     I    I I I    I
            //                         I  I  I
            //                           V6
            // （VはVertexとその番号で、IはIndex順に引いた線のこと）
            // 
            // 上記の形状は半透明Vignetteも不透明Vignetteも同じ
            // ただ、半透明Vignetteが不透明Vignetteの一番奥の四角形(実際には円)に当てはまるように
            // CalculateVignetteScaleAndOffsetで計算してShaderに渡すので、
            // 半透明Vignetteと不透明Vignetteが分かられてVignetteを描画することができる
        }

        if (_opaqueMesh != null)
        {
            DestroyImmediate(_opaqueMesh);
        }

        if (_transparentMesh != null)
        {
            DestroyImmediate(_transparentMesh);
        }

        _opaqueMesh = new Mesh()
        {
            name = "Opaque Vignette Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };
        _transparentMesh = new Mesh()
        {
            name = "Transparent Vignette Mesh",
            hideFlags = HideFlags.HideAndDontSave
        };

        _opaqueMesh.vertices = outerVerts;
        _opaqueMesh.uv = outerUVs;
        _opaqueMesh.triangles = tris;
        _opaqueMesh.UploadMeshData(true);
        _opaqueMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        _opaqueMeshFilter.sharedMesh = _opaqueMesh;

        _transparentMesh.vertices = innerVerts;
        _transparentMesh.uv = innerUVs;
        _transparentMesh.triangles = tris;
        _transparentMesh.UploadMeshData(true);
        _transparentMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        _transparentMeshFilter.sharedMesh = _transparentMesh;
    }
    
    private void BuildMaterials()
    {
#if UNITY_EDITOR
        _InitialFalloff = falloff;
#endif
        if (vignetteShader == null)
        {
            vignetteShader = Shader.Find("App/Vignette");
        }

        if (vignetteShader == null)
        {
            Debug.LogError("Could not find Vignette Shader! Vignette will not be drawn!");
            return;
        }

        if (_opaqueMaterial == null)
        {
            _opaqueMaterial = new Material(vignetteShader)
            {
                name = "Opaque Vignette Material",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Background
            };
            _opaqueMaterial.SetFloat("_BlendSrc", (float)BlendMode.One);
            _opaqueMaterial.SetFloat("_BlendDst", (float)BlendMode.Zero);
            _opaqueMaterial.SetFloat("_ZWrite", 1);
        }

        _opaqueMeshRenderer.sharedMaterial = _opaqueMaterial;

        if (_transparentMaterial == null)
        {
            _transparentMaterial = new Material(vignetteShader)
            {
                name = "Transparent Vignette Material",
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = (int)RenderQueue.Overlay
            };

            _transparentMaterial.SetFloat("_BlendSrc", (float)BlendMode.SrcAlpha);
            _transparentMaterial.SetFloat("_BlendDst", (float)BlendMode.OneMinusSrcAlpha);
            _transparentMaterial.SetFloat("_ZWrite", 0);
        }

        if (falloff == FalloffType.Quadratic)
        {
            _transparentMaterial.EnableKeyword(QUADRATIC_FALLOFF);
        }
        else
        {
            _transparentMaterial.DisableKeyword(QUADRATIC_FALLOFF);
        }

        _transparentMeshRenderer.sharedMaterial = _transparentMaterial;
    }
    
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        DisableRenderers();
    }
    
    public void Init(Camera camera)
    {
        _camera = camera;

        _shaderScaleAndOffset0Property = Shader.PropertyToID("_ScaleAndOffset0");
        _shaderScaleAndOffset1Property = Shader.PropertyToID("_ScaleAndOffset1");

        GameObject opaqueObject = new GameObject("Opaque Vignette") { hideFlags = HideFlags.HideAndDontSave };
        opaqueObject.transform.SetParent(_camera.transform, false);
        _opaqueMeshFilter = opaqueObject.AddComponent<MeshFilter>();
        _opaqueMeshRenderer = opaqueObject.AddComponent<MeshRenderer>();

        _opaqueMeshRenderer.receiveShadows = false;
        _opaqueMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _opaqueMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _opaqueMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _opaqueMeshRenderer.allowOcclusionWhenDynamic = false;
        _opaqueMeshRenderer.enabled = false;

        GameObject transparentObject = new GameObject("Transparent Vignette") { hideFlags = HideFlags.HideAndDontSave };
        transparentObject.transform.SetParent(_camera.transform, false);
        _transparentMeshFilter = transparentObject.AddComponent<MeshFilter>();
        _transparentMeshRenderer = transparentObject.AddComponent<MeshRenderer>();

        _transparentMeshRenderer.receiveShadows = false;
        _transparentMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _transparentMeshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _transparentMeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _transparentMeshRenderer.allowOcclusionWhenDynamic = false;
        _transparentMeshRenderer.enabled = false;

        _innerScaleX.Initialize();
        _innerScaleY.Initialize();
        _middleScaleX.Initialize();
        _middleScaleY.Initialize();
        _outerScaleX.Initialize();
        _outerScaleY.Initialize();
        _offsetX.Initialize();
        _offsetY.Initialize();
        _maxVignetteRange[0] = 1.0f;
        _maxVignetteRange[1] = 1.0f;
        
        BuildMeshes();
        BuildMaterials();
    }

    private void Update()
    {

#if UNITY_EDITOR
    if (meshComplexity != _InitialMeshComplexity)
    {
        // rebuild meshes
        BuildMeshes();
    }

    if (falloff != _InitialFalloff)
    {
        // rebuild materials
        BuildMaterials();
    }
#endif 
        // Nullチェック
        if (_opaqueMaterial == null)
        {
            return;
        }
        
        // Vignettingチェック
        if (!_isAnimating)
        {
            return;
        }
        
        // 初期化
        _transparentVignetteVisible = false;
        _opaqueVignetteVisible = false;

        // Vignetteのrate計算
        var deltaTime = Time.deltaTime;
        if (_targetRate > _rate)
        {
            //表示処理
            var rate = _rate + deltaTime / _showIntervalTime;
            _rate = Mathf.Clamp(rate, 0f, 1f);

            if (_rate >= _targetRate)
            {
                _isAnimating = false;
            }
        }
        else
        {
            //非表示処理
            var rate = _rate - deltaTime / _hideIntervalTime;
            _rate = Mathf.Clamp(rate, 0f, 1f);

            if (_rate <= _targetRate)
            {
                _isAnimating = false;
            }
        }

        SetVignetteMaterial();
    }

    private void CalculateVignetteScaleAndOffset()
    {
        var tanInnerFovY = Mathf.Tan(vignetteFieldOfView * Mathf.Deg2Rad * 0.5f);
        var tanInnerFovX = tanInnerFovY * vignetteAspectRatio;
        var tanMiddleFovX = Mathf.Tan((vignetteFieldOfView + vignetteFalloffDegrees) * Mathf.Deg2Rad * 0.5f);
        var tanMiddleFovY = tanMiddleFovX * vignetteAspectRatio;
        
        for (int i = 0; i < 2; i++)
        {
            float tanFovX, tanFovY;
            if (_camera.stereoEnabled)
            {
                GetTanFovAndOffsetForStereoEye((Camera.StereoscopicEye)i, out tanFovX, out tanFovY, out _offsetX[i], out _offsetY[i]);
            }
            else
            {
                GetTanFovAndOffsetForMonoEye(out tanFovX, out tanFovY, out _offsetX[i], out _offsetY[i]);
            }

            float borderScale = new Vector2((1 + Mathf.Abs(_offsetX[i])) / vignetteAspectRatio, 1 + Mathf.Abs(_offsetY[i])).magnitude * 1.01f;

            _innerScaleX[i] = tanInnerFovX / tanFovX;
            _innerScaleY[i] = tanInnerFovY / tanFovY;
            _middleScaleX[i] = tanMiddleFovX / tanFovX;
            _middleScaleY[i] = tanMiddleFovY / tanFovY;
            _outerScaleX[i] = borderScale * vignetteAspectRatio;
            _outerScaleY[i] = borderScale;

            // Vignette非表示時の最大値
            _maxVignetteRange[i] = new Vector2((1 + Mathf.Abs(_offsetX[i])) / _innerScaleX[i], (1 + Mathf.Abs(_offsetY[i])) / _innerScaleY[i]).magnitude;
        }
    }

    private void SetVignetteMaterial()
    {
        for (var i = 0; i < 2; i++)
        {
            var vignette = 1f - _rate;
            vignette = Remap(vignette, 0f, 1f, 1f, _maxVignetteRange[i]);
            
            // VignetteのIn、Out
            var innerScaleX = _innerScaleX[i] * vignette;
            var innerScaleY = _innerScaleY[i] * vignette;
            var middleScaleX = _middleScaleX[i] * vignette;
            var middleScaleY = _middleScaleY[i] * vignette;
            var outerScaleX = _outerScaleX[i];
            var outerScaleY = _outerScaleY[i];
            var offsetX = _offsetX[i];
            var offsetY = _offsetY[i];

            _OpaqueScaleAndOffset0[i] = new Vector4(outerScaleX, outerScaleY, offsetX, offsetY);
            _OpaqueScaleAndOffset1[i] = new Vector4(middleScaleX, middleScaleY, offsetX, offsetY);
            middleScaleX *= middleOffset;
            middleScaleY *= middleOffset;
            _TransparentScaleAndOffset0[i] = new Vector4(middleScaleX, middleScaleY, offsetX, offsetY);
            _TransparentScaleAndOffset1[i] = new Vector4(innerScaleX, innerScaleY, offsetX, offsetY);
            
            // Vignetteの表示チェック
            _transparentVignetteVisible |= VisibilityTest(_innerScaleX[i], _innerScaleY[i], _offsetX[i], _offsetY[i]);
            _opaqueVignetteVisible |= VisibilityTest(_middleScaleX[i], _middleScaleY[i], _offsetX[i], _offsetY[i]);
        }
        
        // VignetteFalloffDegreesが0以下だと、透明なメッシュを描画する必要がない
        _transparentVignetteVisible &= vignetteFalloffDegrees > 0.0f;
        
        _opaqueMaterial.SetVectorArray(_shaderScaleAndOffset0Property, _OpaqueScaleAndOffset0);
        _opaqueMaterial.SetVectorArray(_shaderScaleAndOffset1Property, _OpaqueScaleAndOffset1);
        _opaqueMaterial.color = vignetteColor;
        _transparentMaterial.SetVectorArray(_shaderScaleAndOffset0Property, _TransparentScaleAndOffset0);
        _transparentMaterial.SetVectorArray(_shaderScaleAndOffset1Property, _TransparentScaleAndOffset1);
        _transparentMaterial.color = vignetteColor;
    }

    public void VignetteIn()
    {
        if (_rate >= 1f)
        {
            //既に表示されていたら抜ける
            if (!_isVignetteStart)
            {
                _isVignetteStart = true;
            }
            return;
        }
        _targetRate = 1f;
        _isAnimating = true;
        
        if (_isVignetteStart)
        {
            CalculateVignetteScaleAndOffset();
            _isVignetteStart = false;
        }
    }
    
    public void VignetteOut()
    {
        if (_rate <= 0f)
        {
            //既に非表示だったら抜ける
            if (!_isVignetteStart)
            {
                _isVignetteStart = true;
            }
            return;
        }
        _targetRate = 0f;
        _isAnimating = true;
        
        if (_isVignetteStart)
        {
            CalculateVignetteScaleAndOffset();
            _isVignetteStart = false;
        }
    }

    private void GetTanFovAndOffsetForStereoEye(Camera.StereoscopicEye eye, out float tanFovX, out float tanFovY, out float offsetX, out float offsetY)
    {
        // VRの場合のTangent FOV、Offsetを計算
        var pt = _camera.GetStereoProjectionMatrix(eye).transpose;

        var right = pt * new Vector4(-1, 0, 0, 1);
        var left = pt * new Vector4(1, 0, 0, 1);
        var up = pt * new Vector4(0, -1, 0, 1);
        var down = pt * new Vector4(0, 1, 0, 1);

        var rightTanFovX = right.z / right.x;
        var leftTanFovX = left.z / left.x;
        var upTanFovY = up.z / up.y;
        var downTanFovY = down.z / down.y;

        offsetX = -(rightTanFovX + leftTanFovX) / 2;
        offsetY = -(upTanFovY + downTanFovY) / 2;

        tanFovX = (rightTanFovX - leftTanFovX) / 2;
        tanFovY = (upTanFovY - downTanFovY) / 2;
    }

    private void GetTanFovAndOffsetForMonoEye(out float tanFovX, out float tanFovY, out float offsetX, out float offsetY)
    {
        // VRではない場合のTangent FOV計算
        tanFovY = Mathf.Tan(Mathf.Deg2Rad * _camera.fieldOfView * 0.5f);
        tanFovX = tanFovY * _camera.aspect;
        offsetX = 0f;
        offsetY = 0f;
    }

    private static bool VisibilityTest(float scaleX, float scaleY, float offsetX, float offsetY)
    {
        // Vignetteが見えている状態かチェックする
        // ViewportのコーナーがVignetteから一番遠いため、ViewportのコーナーがVignetteの外にあるかどうかチェックするだけで良い
        return new Vector2((1 + Mathf.Abs(offsetX)) / scaleX, (1 + Mathf.Abs(offsetY)) / scaleY).sqrMagnitude > 1.0f;
    }
    
    private static float Remap(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + (val - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    private void EnableRenderers()
    {
        _opaqueMeshRenderer.enabled = _opaqueVignetteVisible;
        _transparentMeshRenderer.enabled = _transparentVignetteVisible;
    }

    private void DisableRenderers()
    {
        _opaqueMeshRenderer.enabled = false;
        _transparentMeshRenderer.enabled = false;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera == _camera)
        {
            EnableRenderers();
        }
        else
        {
            DisableRenderers();
        }
    }
}
