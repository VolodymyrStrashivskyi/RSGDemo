using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class PuddleSimulation
    {
        private const float EQUALIZE_SHARE = 0.5f;

        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        private readonly int _resolution;
        private readonly float _cellArea;
        private readonly float[] _ground;
        private readonly bool[] _drains;
        private readonly float[] _water;
        private readonly float[] _delta;
        private readonly float[] _drops = new float[4];

        public PuddleSimulation(int resolution, float cellSize, float[] ground, bool[] drains)
        {
            _resolution = resolution;
            _cellArea = cellSize * cellSize;
            _ground = ground;
            _drains = drains;
            _water = new float[resolution * resolution];
            _delta = new float[resolution * resolution];
        }

        public float[] Water => _water;

        public float TotalDepth { get; private set; }

        public void Pour(Vector2 cell, float volume, float radiusInCells)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cell.x - radiusInCells));
            int maxX = Mathf.Min(_resolution - 1, Mathf.CeilToInt(cell.x + radiusInCells));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cell.y - radiusInCells));
            int maxY = Mathf.Min(_resolution - 1, Mathf.CeilToInt(cell.y + radiusInCells));
            float weightSum = 0f;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                    weightSum += GetPourWeight(cell, x, y, radiusInCells);
            }

            if (weightSum <= 0f)
                return;

            float depthPerWeight = volume / (_cellArea * weightSum);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                    _water[y * _resolution + x] += GetPourWeight(cell, x, y, radiusInCells) * depthPerWeight;
            }
        }

        public void Step(float deltaTime, float flowRate, float minFlowDepth, float evaporation)
        {
            Array.Clear(_delta, 0, _delta.Length);
            float flowShare = Mathf.Clamp01(flowRate * deltaTime) * EQUALIZE_SHARE;

            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                    FlowOut(x, y, flowShare, minFlowDepth);
            }

            float total = 0f;
            float evaporated = evaporation * deltaTime;

            for (int i = 0; i < _water.Length; i++)
            {
                _water[i] = _drains[i] ? 0f : Mathf.Max(0f, _water[i] + _delta[i] - evaporated);
                total += _water[i];
            }

            TotalDepth = total;
        }

        private float GetPourWeight(Vector2 cell, int x, int y, float radiusInCells)
        {
            float distance = Vector2.Distance(cell, new Vector2(x, y)) / radiusInCells;
            return distance >= 1f ? 0f : 1f - distance * distance;
        }

        private void FlowOut(int x, int y, float flowShare, float minFlowDepth)
        {
            int index = y * _resolution + x;
            float water = _water[index];

            if (water <= minFlowDepth)
                return;

            float surface = _ground[index] + water;
            float dropSum = 0f;

            for (int n = 0; n < Neighbours.Length; n++)
            {
                int neighbourX = x + Neighbours[n].x;
                int neighbourY = y + Neighbours[n].y;
                float neighbourSurface = IsInside(neighbourX, neighbourY)
                    ? _ground[neighbourY * _resolution + neighbourX] + _water[neighbourY * _resolution + neighbourX]
                    : _ground[index];

                _drops[n] = Mathf.Max(0f, surface - neighbourSurface);
                dropSum += _drops[n];
            }

            if (dropSum <= 0f)
                return;

            float moved = Mathf.Min(water - minFlowDepth, dropSum) * flowShare;
            _delta[index] -= moved;

            for (int n = 0; n < Neighbours.Length; n++)
            {
                int neighbourX = x + Neighbours[n].x;
                int neighbourY = y + Neighbours[n].y;

                if (_drops[n] > 0f && IsInside(neighbourX, neighbourY))
                    _delta[neighbourY * _resolution + neighbourX] += moved * _drops[n] / dropSum;
            }
        }

        private bool IsInside(int x, int y) =>
            x >= 0 && y >= 0 && x < _resolution && y < _resolution;
    }
}
