using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
namespace App.Common.Views
{
    public class ScreenFadeView : MonoBehaviour
    {
        [SerializeField] private float _fadeTime = 1.0f;
        [SerializeField] private Color _fadeColor = new Color(0.01f, 0.01f, 0.01f, 1.0f);
        [SerializeField] private int _renderQueue = 3500;

        private MeshRenderer _fadeRenderer;
        private MeshFilter _fadeMesh;
        private Material _fadeMaterial = null;
        private bool _isFading = false;
        private float _currentAlpha;

        void Start()
        {
            // Fade용 머티리얼 작성(셰이더는 아래에 별도 기재)
            _fadeMaterial = new Material(Shader.Find("App/ScreenFade"));
            _fadeMesh = gameObject.AddComponent<MeshFilter>();
            _fadeRenderer = gameObject.AddComponent<MeshRenderer>();
            _fadeRenderer.material = _fadeMaterial;

            // 카메라를 덮는 Fade용 Mesh를 작성
            var mesh = new Mesh();
            _fadeMesh.mesh = mesh;
            Vector3[] vertices = new Vector3[4];
            float width = 2f;
            float height = 2f;
            float depth = 1f;
            // 버텍스
            vertices[0] = new Vector3(-width, -height, depth);
            vertices[1] = new Vector3(width, -height, depth);
            vertices[2] = new Vector3(-width, height, depth);
            vertices[3] = new Vector3(width, height, depth);
            mesh.vertices = vertices;
            // 인덱스
            int[] indices = new int[6];
            indices[0] = 0; indices[1] = 2; indices[2] = 1;
            indices[3] = 2; indices[4] = 3; indices[5] = 1;
            mesh.triangles = indices;
            // 노멀
            Vector3[] normals = new Vector3[4];
            normals[0] = -Vector3.forward;
            normals[1] = -Vector3.forward;
            normals[2] = -Vector3.forward;
            normals[3] = -Vector3.forward;
            mesh.normals = normals;
            // UV
            Vector2[] uv = new Vector2[4];
            uv[0] = new Vector2(0, 0);
            uv[1] = new Vector2(1, 0);
            uv[2] = new Vector2(0, 1);
            uv[3] = new Vector2(1, 1);
            mesh.uv = uv;

            // Fade 알파
            _currentAlpha = 0.0f;
        }

        private async UniTask FadeInAsync()
        {
            await Fade(1f, 0f);
        }

        public void FadeIn()
        {
            FadeInAsync().Forget();
        }

        private async UniTask FadeOutAsync()
        {
            await Fade(0f, 1f);
        }

        public void FadeOut()
        {
            FadeOutAsync().Forget();
        }

        /// <summary>
        /// 머티리얼의 알파를 조정해서 Fade시키는 함수
        /// 1.0 ~ 0.0으로 설정하면 Fade out
        /// 0.0 ~ 1.0으로 설정하면 Fade in
        /// </summary>
        private async UniTask Fade(float startAlpha, float endAlpha)
        {
            var elapsedTime = 0.0f;
            while (elapsedTime < _fadeTime)
            {
                elapsedTime += Time.deltaTime;
                _currentAlpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(elapsedTime / _fadeTime));
                SetMaterial();
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
            }
            _currentAlpha = endAlpha;
            SetMaterial();
        }

        /// <summary>
        /// 머티리얼의 알파 값을 조정해서 Fade를 실시
        /// </summary>
        private void SetMaterial()
        {
            Color color = _fadeColor;
            color.a = _currentAlpha;
            _isFading = color.a > 0;
            if (_fadeMaterial != null)
            {
                _fadeMaterial.color = color;
                _fadeMaterial.renderQueue = _renderQueue;
                _fadeRenderer.material = _fadeMaterial;
                _fadeRenderer.enabled = _isFading;
            }
        }
    }
}