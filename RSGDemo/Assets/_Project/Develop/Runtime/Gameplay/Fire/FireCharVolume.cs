using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireCharVolume : IFireCharVolume, ITickable, ILateTickable, IDisposable
    {
        private const float MIN_WEIGHT = 0.01f;
        private const int BORDER_CELLS = 1;
        private const float HALF = 0.5f;
        private const int CELLS_PER_JOB_BATCH = 256;

        private static readonly int _volumeId = Shader.PropertyToID("_CharVolume");
        private static readonly int _matrixId = Shader.PropertyToID("_CharVolumeMatrix");
        private static readonly int _enabledId = Shader.PropertyToID("_CharVolumeEnabled");

        private readonly IFireSystem _fireSystem;
        private readonly FireVisualConfig _config;
        private FireSpot[] _spots = new FireSpot[0];
        private NativeArray<float> _spotChars;
        private NativeArray<float> _spotHeats;
        private NativeArray<int> _cellStarts;
        private NativeArray<int> _entrySpots;
        private NativeArray<float> _entryWeights;
        private NativeArray<ushort> _pixels;
        private JobHandle _job;
        private bool _isJobRunning;
        private Texture3D _texture;
        private float _timeToUpdate;

        public FireCharVolume(IFireSystem fireSystem, FireVisualConfig config)
        {
            _fireSystem = fireSystem;
            _config = config;
        }

        public void Build(Transform space)
        {
            _spots = new FireSpot[_fireSystem.Spots.Count];

            for (int i = 0; i < _spots.Length; i++)
                _spots[i] = _fireSystem.Spots[i];

            _spotChars = new NativeArray<float>(_spots.Length, Allocator.Persistent);
            _spotHeats = new NativeArray<float>(_spots.Length, Allocator.Persistent);

            Matrix4x4 worldToLocal = space.worldToLocalMatrix;
            float cellSize = _config.CharVoxelSize;
            Bounds bounds = FindLocalBounds(worldToLocal);
            bounds.Expand(cellSize * (BORDER_CELLS + BORDER_CELLS));
            Vector3Int cells = Vector3Int.CeilToInt(bounds.size / cellSize);
            Vector3 origin = bounds.min;

            BuildEntries(space.localToWorldMatrix, worldToLocal, origin, cells, cellSize);

            _texture = new Texture3D(cells.x, cells.y, cells.z, TextureFormat.RG16, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "FireCharVolume"
            };
            _pixels = new NativeArray<ushort>(cells.x * cells.y * cells.z, Allocator.Persistent);

            Vector3 volumeSize = (Vector3)cells * cellSize;
            Matrix4x4 localToVolume = Matrix4x4.Scale(new Vector3(1f / volumeSize.x, 1f / volumeSize.y, 1f / volumeSize.z))
                * Matrix4x4.Translate(-origin);

            Shader.SetGlobalTexture(_volumeId, _texture);
            Shader.SetGlobalMatrix(_matrixId, localToVolume * worldToLocal);
            Shader.SetGlobalFloat(_enabledId, 1f);

            ScheduleRefresh();
            UploadRefresh();
        }

        public void Tick()
        {
            if (_texture == null)
                return;

            _timeToUpdate -= Time.deltaTime;

            if (_timeToUpdate > 0f)
                return;

            _timeToUpdate = 1f / _config.CharUpdateRate;
            ScheduleRefresh();
        }

        public void LateTick() =>
            UploadRefresh();

        public void Dispose()
        {
            _job.Complete();
            Shader.SetGlobalFloat(_enabledId, 0f);

            if (_texture != null)
                Object.Destroy(_texture);

            DisposeArray(_spotChars);
            DisposeArray(_spotHeats);
            DisposeArray(_cellStarts);
            DisposeArray(_entrySpots);
            DisposeArray(_entryWeights);
            DisposeArray(_pixels);
        }

        private Bounds FindLocalBounds(Matrix4x4 worldToLocal)
        {
            Bounds bounds = new Bounds(worldToLocal.MultiplyPoint3x4(_spots[0].Position), Vector3.zero);

            foreach (FireSpot spot in _spots)
            {
                float reach = FireCharLook.GetReach(spot, _config);
                float side = spot.Radius + reach;
                Vector3 bottom = worldToLocal.MultiplyPoint3x4(spot.Position);
                bounds.Encapsulate(bottom - new Vector3(side, reach, side));
                bounds.Encapsulate(bottom + new Vector3(side, spot.Height + reach, side));
            }

            return bounds;
        }

        private void BuildEntries(Matrix4x4 localToWorld, Matrix4x4 worldToLocal, Vector3 origin, Vector3Int cells, float cellSize)
        {
            int cellCount = cells.x * cells.y * cells.z;
            List<int>[] cellSpots = new List<int>[cellCount];
            List<float>[] cellWeights = new List<float>[cellCount];
            int entryCount = 0;

            for (int spotIndex = 0; spotIndex < _spots.Length; spotIndex++)
            {
                FireSpot spot = _spots[spotIndex];

                if (spot.IsMovable)
                    continue;

                float reach = FireCharLook.GetReach(spot, _config);
                float side = spot.Radius + reach;
                Vector3 bottom = worldToLocal.MultiplyPoint3x4(spot.Position);
                Vector3Int min = ToCell(bottom - new Vector3(side, reach, side), origin, cellSize, cells);
                Vector3Int max = ToCell(bottom + new Vector3(side, spot.Height + reach, side), origin, cellSize, cells);

                for (int z = min.z; z <= max.z; z++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int x = min.x; x <= max.x; x++)
                        {
                            Vector3 center = origin + (new Vector3(x, y, z) + Vector3.one * HALF) * cellSize;
                            float gap = spot.GetDistanceTo(localToWorld.MultiplyPoint3x4(center));
                            float weight = 1f - gap / reach;

                            if (weight < MIN_WEIGHT)
                                continue;

                            int cell = x + cells.x * (y + cells.y * z);
                            cellSpots[cell] ??= new List<int>();
                            cellWeights[cell] ??= new List<float>();
                            cellSpots[cell].Add(spotIndex);
                            cellWeights[cell].Add(weight);
                            entryCount++;
                        }
                    }
                }
            }

            PackEntries(cellSpots, cellWeights, entryCount);
        }

        private void PackEntries(List<int>[] cellSpots, List<float>[] cellWeights, int entryCount)
        {
            _cellStarts = new NativeArray<int>(cellSpots.Length + 1, Allocator.Persistent);
            _entrySpots = new NativeArray<int>(entryCount, Allocator.Persistent);
            _entryWeights = new NativeArray<float>(entryCount, Allocator.Persistent);
            int next = 0;

            for (int cell = 0; cell < cellSpots.Length; cell++)
            {
                _cellStarts[cell] = next;

                if (cellSpots[cell] == null)
                    continue;

                for (int i = 0; i < cellSpots[cell].Count; i++)
                {
                    _entrySpots[next] = cellSpots[cell][i];
                    _entryWeights[next] = cellWeights[cell][i];
                    next++;
                }
            }

            _cellStarts[cellSpots.Length] = next;
        }

        private static Vector3Int ToCell(Vector3 local, Vector3 origin, float cellSize, Vector3Int cells)
        {
            Vector3Int cell = Vector3Int.FloorToInt((local - origin) / cellSize);
            return Vector3Int.Max(Vector3Int.zero, Vector3Int.Min(cells - Vector3Int.one, cell));
        }

        private void ScheduleRefresh()
        {
            if (_isJobRunning)
                return;

            for (int i = 0; i < _spots.Length; i++)
            {
                _spotChars[i] = FireCharLook.GetShownChar(_spots[i], _config);
                _spotHeats[i] = _spots[i].GetGlow(_config.EmberLightShare);
            }

            FireCharVolumeJob job = new FireCharVolumeJob
            {
                CellStarts = _cellStarts,
                EntrySpots = _entrySpots,
                EntryWeights = _entryWeights,
                SpotChars = _spotChars,
                SpotHeats = _spotHeats,
                Pixels = _pixels
            };

            _job = job.Schedule(_pixels.Length, CELLS_PER_JOB_BATCH);
            _isJobRunning = true;
        }

        private void UploadRefresh()
        {
            if (_isJobRunning == false)
                return;

            _job.Complete();
            _isJobRunning = false;
            _texture.SetPixelData(_pixels, 0);
            _texture.Apply(false, false);
        }

        private static void DisposeArray<T>(NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
        }
    }
}
