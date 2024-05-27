using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace App.Battle.Views
{
    public struct InstanceData
    {
        public Vector3 Position;
        public float Keta;
        public uint Number;
        public float EndTime;
        public float Hue;
    }

    public record PopupData
    {
        public Vector3 Position { get; set; }
        public int Number { get; set; }
        public float EndTime { get; set; }
        public float Hue { get; set; }
    }

    public class DamagePopupView : MonoBehaviour
    {
        [SerializeField] private Mesh _mesh;
        [SerializeField] private Material _material;
        private Material _copiedMaterial;
        private const int MaxPopup = 200;
        private const int MaxColumn = 5;
        private PopupData[] _data = new PopupData[MaxPopup];

        public void Add(float number, Vector3 position, float hue)
        {
            AddData(number, position, hue);
            UpdateBuffers();
        }

        private void AddData(float number, Vector3 position, float hue)
        {
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i].Number == 0 || _data[i].EndTime < Time.time)
                {
                    _data[i] = _data[i] with
                    {
                        Number = (int) number,
                        Position = position,
                        EndTime = Time.time + 1,
                        Hue = hue
                    };
                    return;
                }
            }
            Debug.LogWarning("Too many damage popup");
        }


        private ComputeBuffer instancingBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args = new uint[5] {0, 0, 0, 0, 0};

        void Start()
        {
            for (var i = 0; i < _data.Length; i++)
            {
                if (_data[i] == null)
                {
                    _data[i] = new PopupData();
                }
            }

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            _copiedMaterial = new Material(_material);
            UpdateBufferSize();
            UpdateBuffers();
        }

        void Update()
        {
            if (SystemInfo.supportsInstancing)
            {
                Graphics.DrawMeshInstancedIndirect(
                    _mesh,
                    0,
                    _copiedMaterial,
                    new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
                    argsBuffer,
                    lightProbeUsage: LightProbeUsage.Off,
                    castShadows: ShadowCastingMode.Off,
                    receiveShadows: false);
            }
        }

        void UpdateBufferSize()
        {
            if (instancingBuffer != null)
                instancingBuffer.Release();
            instancingBuffer = new ComputeBuffer(MaxPopup * MaxColumn, UnsafeUtility.SizeOf<InstanceData>());
            _copiedMaterial.SetBuffer("instancingBuffer", instancingBuffer);
        }

        void UpdateBuffers()
        {
            var instances = new NativeArray<InstanceData>(MaxPopup*MaxColumn, Allocator.Temp);
            var numIndex = 0;
            for (var i = 0; i < MaxPopup; i++)
            {
                var data = _data[i];
                if (data.Number == 0)
                {
                    continue;
                }

                var keta = Mathf.FloorToInt(Mathf.Log10(data.Number));
                var number = data.Number;
                for(var j = 0; j <= keta; j++)
                {
                    instances[numIndex++] = new InstanceData
                    {
                        Position = data.Position,
                        Keta = j - keta / 2f,
                        Number = (uint) number,
                        EndTime = data.EndTime,
                        Hue = data.Hue
                    };
                    number /= 10;
                }
            }

            instancingBuffer.SetData(instances);
            _copiedMaterial.SetBuffer("instancingBuffer", instancingBuffer);

            // indirect args
            uint numIndices = (_mesh != null) ? (uint) _mesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint) numIndex * 2;
            argsBuffer.SetData(args);
        }

        void OnDisable()
        {
            if (instancingBuffer != null)
                instancingBuffer.Release();
            instancingBuffer = null;

            if (argsBuffer != null)
                argsBuffer.Release();
            argsBuffer = null;

            Destroy(_copiedMaterial);
            _copiedMaterial = null;
        }
    }
}