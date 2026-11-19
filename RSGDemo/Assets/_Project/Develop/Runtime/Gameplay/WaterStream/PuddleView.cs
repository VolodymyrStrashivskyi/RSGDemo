using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class PuddleView : MonoBehaviour
    {
        private const float GROUND_PROBE_HEIGHT = 3f;
        private const float GROUND_PROBE_DISTANCE = 8f;
        private const float MIN_VISIBLE_DEPTH = 1e-5f;
        private const float HALF = 0.5f;

        private static readonly int WetnessId = Shader.PropertyToID("_WetnessTex");
        private static readonly int FullDepthId = Shader.PropertyToID("_FullDepth");
        private static readonly int ImpactPositionId = Shader.PropertyToID("_ImpactPosition");
        private static readonly int ActivityId = Shader.PropertyToID("_Activity");

        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _renderer;

        private WaterStreamConfig _config;
        private IRaycastService _raycastService;
        private PuddleSimulation _simulation;
        private Texture2D _wetness;
        private Mesh _mesh;
        private MaterialPropertyBlock _properties;
        private Vector3 _impactPosition;
        private float _targetActivity;
        private float _activity;
        private float _stepAccumulator;

        [Inject]
        public void Construct(WaterStreamConfig config, IRaycastService raycastService)
        {
            _config = config;
            _raycastService = raycastService;
            _properties = new MaterialPropertyBlock();
            _renderer.enabled = false;
        }

        public void Pour(Vector3 point, float volume)
        {
            EnsureBuilt();
            Vector3 local = transform.InverseTransformPoint(point);
            float half = _config.PuddleResolution * HALF;
            Vector2 cell = new Vector2(local.x / _config.PuddleCellSize + half, local.z / _config.PuddleCellSize + half) - Vector2.one * HALF;
            _simulation.Pour(cell, volume, _config.PuddlePourRadius / _config.PuddleCellSize);
        }

        public void SetImpact(Vector3 point, bool isLanding)
        {
            if (isLanding)
                _impactPosition = point;

            _targetActivity = isLanding ? 1f : 0f;
        }

        private void Update()
        {
            if (_simulation == null)
                return;

            float deltaTime = Time.deltaTime;
            _activity = Mathf.MoveTowards(_activity, _targetActivity, deltaTime / _config.PuddleActivityFadeTime);
            _stepAccumulator += deltaTime;
            bool hasStepped = false;

            while (_stepAccumulator >= _config.PuddleSimulationStep)
            {
                _stepAccumulator -= _config.PuddleSimulationStep;
                _simulation.Step(_config.PuddleSimulationStep, _config.PuddleFlowRate, _config.PuddleMinFlowDepth, _config.PuddleEvaporation);
                hasStepped = true;
            }

            if (hasStepped)
            {
                _wetness.SetPixelData(_simulation.Water, 0);
                _wetness.Apply(false);
            }

            _renderer.enabled = _simulation.TotalDepth > MIN_VISIBLE_DEPTH;
            _properties.SetTexture(WetnessId, _wetness);
            _properties.SetFloat(FullDepthId, _config.PuddleFullDepth);
            _properties.SetVector(ImpactPositionId, _impactPosition);
            _properties.SetFloat(ActivityId, _activity);
            _renderer.SetPropertyBlock(_properties);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);

            if (_wetness != null)
                Destroy(_wetness);
        }

        private void EnsureBuilt()
        {
            if (_simulation != null)
                return;

            PlaceArea();
            int resolution = _config.PuddleResolution;
            float[] ground = new float[resolution * resolution];
            _mesh = BuildGroundMesh(ground);
            _meshFilter.sharedMesh = _mesh;
            _simulation = new PuddleSimulation(resolution, _config.PuddleCellSize, ground);
            _wetness = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true)
            {
                name = "PuddleWetness",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private void PlaceArea()
        {
            Transform stream = transform.parent;
            Vector3 forward = Vector3.ProjectOnPlane(stream.forward, Vector3.up).normalized;
            Vector3 center = stream.position + forward * _config.PuddleForwardOffset;
            transform.SetPositionAndRotation(new Vector3(center.x, 0f, center.z), Quaternion.LookRotation(forward));
            transform.localScale = Vector3.one;
        }

        private Mesh BuildGroundMesh(float[] ground)
        {
            int resolution = _config.PuddleResolution;
            float cellSize = _config.PuddleCellSize;
            float half = (resolution - 1) * HALF;
            List<Vector3> vertices = new List<Vector3>(resolution * resolution);
            List<Vector2> uvs = new List<Vector2>(resolution * resolution);
            List<int> triangles = new List<int>();
            float fallbackHeight = transform.parent.position.y;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector3 world = transform.TransformPoint(new Vector3((x - half) * cellSize, 0f, (y - half) * cellSize));
                    float height = FindGroundHeight(world, fallbackHeight);
                    ground[y * resolution + x] = height + GetGroundNoise(world);
                    vertices.Add(transform.InverseTransformPoint(new Vector3(world.x, height + _config.PuddleLift, world.z)));
                    uvs.Add(new Vector2((x + HALF) / resolution, (y + HALF) / resolution));
                }
            }

            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int corner = y * resolution + x;
                    triangles.Add(corner);
                    triangles.Add(corner + resolution);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + 1);
                    triangles.Add(corner + resolution);
                    triangles.Add(corner + resolution + 1);
                }
            }

            Mesh mesh = new Mesh { name = "PuddleGround", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private float FindGroundHeight(Vector3 world, float fallbackHeight)
        {
            Ray ray = new Ray(new Vector3(world.x, fallbackHeight + GROUND_PROBE_HEIGHT, world.z), Vector3.down);
            return _raycastService.TryRaycastFarthest(ray, GROUND_PROBE_DISTANCE, _config.PuddleGroundMask, out RaycastHit hit) ? hit.point.y : fallbackHeight;
        }

        private float GetGroundNoise(Vector3 world) =>
            (Mathf.PerlinNoise(world.x * _config.PuddleNoiseScale, world.z * _config.PuddleNoiseScale) - HALF) * _config.PuddleGroundNoise;
    }
}
