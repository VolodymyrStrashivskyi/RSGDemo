using System.Collections.Generic;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class PuddleView : MonoBehaviour
    {
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

        public bool IsPlaced => _simulation != null;

        public float TotalDepth => IsPlaced ? _simulation.TotalDepth : 0f;

        public void Place(Vector3 floorPoint)
        {
            transform.SetPositionAndRotation(floorPoint, Quaternion.identity);
            transform.localScale = Vector3.one;
            _activity = 0f;
            _targetActivity = 0f;
            _stepAccumulator = 0f;

            int resolution = _config.PuddleResolution;
            float[] ground = new float[resolution * resolution];
            bool[] drains = new bool[resolution * resolution];

            if (_mesh != null)
                Destroy(_mesh);

            _mesh = BuildGroundMesh(ground, drains, floorPoint.y);
            _meshFilter.sharedMesh = _mesh;
            _simulation = new PuddleSimulation(resolution, _config.PuddleCellSize, ground, drains);

            if (_wetness == null)
            {
                _wetness = new Texture2D(resolution, resolution, TextureFormat.RFloat, false, true)
                {
                    name = "PuddleWetness",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }
        }

        public bool Contains(Vector3 floorPoint, float margin)
        {
            if (IsPlaced == false || Mathf.Abs(floorPoint.y - transform.position.y) > _config.PuddleLevelTolerance)
                return false;

            Vector3 local = transform.InverseTransformPoint(floorPoint);
            float reach = (_config.PuddleResolution - 1) * HALF * _config.PuddleCellSize - margin;
            return Mathf.Abs(local.x) < reach && Mathf.Abs(local.z) < reach;
        }

        public void Pour(Vector3 point, float volume)
        {
            if (IsPlaced == false)
                return;

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

        private Mesh BuildGroundMesh(float[] ground, bool[] drains, float floorHeight)
        {
            int resolution = _config.PuddleResolution;
            float cellSize = _config.PuddleCellSize;
            float half = (resolution - 1) * HALF;
            List<Vector3> vertices = new List<Vector3>(resolution * resolution);
            List<Vector2> uvs = new List<Vector2>(resolution * resolution);
            List<int> triangles = new List<int>();

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    Vector3 world = transform.TransformPoint(new Vector3((x - half) * cellSize, 0f, (y - half) * cellSize));
                    int index = y * resolution + x;
                    bool hasGround = TryFindGroundHeight(world, floorHeight, out float height);
                    drains[index] = hasGround == false;
                    ground[index] = hasGround ? height + GetGroundNoise(world) : floorHeight - _config.PuddleProbeHeight - _config.PuddleProbeDepth;
                    vertices.Add(transform.InverseTransformPoint(new Vector3(world.x, height + _config.PuddleLift, world.z)));
                    uvs.Add(new Vector2((x + HALF) / resolution, (y + HALF) / resolution));
                }
            }

            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int corner = y * resolution + x;

                    if (IsSteep(vertices, corner, resolution) || TouchesDrain(drains, corner, resolution))
                        continue;

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

        private bool IsSteep(List<Vector3> vertices, int corner, int resolution)
        {
            float nearLeft = vertices[corner].y;
            float nearRight = vertices[corner + 1].y;
            float farLeft = vertices[corner + resolution].y;
            float farRight = vertices[corner + resolution + 1].y;
            float rise = Mathf.Max(nearLeft, nearRight, farLeft, farRight) - Mathf.Min(nearLeft, nearRight, farLeft, farRight);
            return rise > _config.PuddleMaxCellRise;
        }

        private static bool TouchesDrain(bool[] drains, int corner, int resolution) =>
            drains[corner] || drains[corner + 1] || drains[corner + resolution] || drains[corner + resolution + 1];

        private bool TryFindGroundHeight(Vector3 world, float floorHeight, out float height)
        {
            Ray ray = new Ray(new Vector3(world.x, floorHeight + _config.PuddleProbeHeight, world.z), Vector3.down);
            float distance = _config.PuddleProbeHeight + _config.PuddleProbeDepth;
            bool isHit = _raycastService.TryRaycast(ray, distance, _config.PuddleGroundMask, out RaycastHit hit);
            height = isHit ? hit.point.y : floorHeight;
            return isHit;
        }

        private float GetGroundNoise(Vector3 world) =>
            (Mathf.PerlinNoise(world.x * _config.PuddleNoiseScale, world.z * _config.PuddleNoiseScale) - HALF) * _config.PuddleGroundNoise;
    }
}
