using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public class HoseTubeMesh
    {
        private const float MIN_SEGMENT_LENGTH = 0.0001f;
        private const float MIN_NORMAL_SQR_MAGNITUDE = 0.000001f;
        private const float RIB_SHARPNESS = 2f;
        private const float HALF = 0.5f;

        private readonly List<Vector3> _centers = new List<Vector3>();
        private readonly List<float> _distances = new List<float>();
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();
        private readonly float _inflatedRadius;
        private readonly float _deflatedRadius;
        private readonly float _deflatedSquash;
        private readonly int _sides;
        private readonly int _subdivisions;
        private readonly Vector2 _paletteUv;
        private readonly float _endTaperLength;

        private float _ribSpacing;
        private float _ribDepth;
        private float _ribMaterialLength;
        private float _ribOffset;
        private bool _isStartFitted = true;
        private bool _isEndFitted = true;
        private bool _hasStartNormal;
        private Vector3 _startNormal;
        private bool _hasStartTangent;
        private Vector3 _startTangent;
        private bool _hasEndTangent;
        private Vector3 _endTangent;

        public HoseTubeMesh(float inflatedRadius, float deflatedRadius, float deflatedSquash, int sides, int subdivisions, Vector2 paletteUv, float endTaperLength)
        {
            _inflatedRadius = inflatedRadius;
            _deflatedRadius = deflatedRadius;
            _deflatedSquash = deflatedSquash;
            _sides = sides;
            _subdivisions = subdivisions;
            _paletteUv = paletteUv;
            _endTaperLength = endTaperLength;

            Mesh = new Mesh { name = "Hose" };
            Mesh.MarkDynamic();
        }

        public Mesh Mesh { get; }

        public Vector3 EndNormal { get; private set; }

        public void SetRibs(float spacing, float depth)
        {
            _ribSpacing = spacing;
            _ribDepth = depth;
        }

        public void SetRibPhase(float materialLength, float offset)
        {
            _ribMaterialLength = materialLength;
            _ribOffset = offset;
        }

        public void SetFittedEnds(bool isStartFitted, bool isEndFitted)
        {
            _isStartFitted = isStartFitted;
            _isEndFitted = isEndFitted;
        }

        public void SetStartNormal(Vector3 normal)
        {
            _startNormal = normal;
            _hasStartNormal = true;
        }

        public void SetStartTangent(Vector3 tangent)
        {
            _startTangent = tangent.normalized;
            _hasStartTangent = true;
        }

        public void SetEndTangent(Vector3 tangent)
        {
            _endTangent = tangent.normalized;
            _hasEndTangent = true;
        }

        public void Build(IReadOnlyList<Vector3> points, Func<float, float> inflationAt)
        {
            Smooth(points);
            ClearBuffers();

            if (_centers.Count < 2)
            {
                Mesh.Clear();
                return;
            }

            AddRings(inflationAt);
            AddTriangles();
            AddCaps();
            ApplyToMesh();
        }

        private void Smooth(IReadOnlyList<Vector3> points)
        {
            _centers.Clear();
            _distances.Clear();

            int lastIndex = points.Count - 1;

            for (int i = 0; i < lastIndex; i++)
            {
                Vector3 before = points[Mathf.Max(i - 1, 0)];
                Vector3 after = points[Mathf.Min(i + 2, lastIndex)];

                for (int step = 0; step < _subdivisions; step++)
                    AddCenter(GetCatmullRom(before, points[i], points[i + 1], after, (float)step / _subdivisions));
            }

            if (points.Count > 0)
                AddCenter(points[lastIndex]);
        }

        private void AddCenter(Vector3 center)
        {
            if (_centers.Count == 0)
            {
                _centers.Add(center);
                _distances.Add(0f);
                return;
            }

            float segmentLength = Vector3.Distance(_centers[_centers.Count - 1], center);

            if (segmentLength < MIN_SEGMENT_LENGTH)
                return;

            _centers.Add(center);
            _distances.Add(_distances[_distances.Count - 1] + segmentLength);
        }

        private void ClearBuffers()
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
        }

        private void AddRings(Func<float, float> inflationAt)
        {
            float length = _distances[_distances.Count - 1];
            Vector3 previousTangent = GetTangent(0);
            Vector3 normal = GetStartNormal(previousTangent);
            float materialScale = _ribMaterialLength > 0f && length > 0f ? _ribMaterialLength / length : 1f;

            for (int i = 0; i < _centers.Count; i++)
            {
                Vector3 tangent = GetTangent(i);
                normal = TransportNormal(normal, previousTangent, tangent);
                previousTangent = tangent;

                Vector3 side = Vector3.Cross(normal, tangent);
                float inflation = length > 0f ? inflationAt(_distances[i] / length) * GetEndTaper(_distances[i], length) : 0f;
                AddRing(_centers[i], side, normal, inflation, GetRibScale((length - _distances[i]) * materialScale + _ribOffset));
            }

            EndNormal = normal;
        }

        private Vector3 GetStartNormal(Vector3 tangent)
        {
            if (_hasStartNormal == false)
                return GetInitialNormal(tangent);

            Vector3 normal = Vector3.ProjectOnPlane(_startNormal, tangent);
            return normal.sqrMagnitude < MIN_NORMAL_SQR_MAGNITUDE ? GetInitialNormal(tangent) : normal.normalized;
        }

        private void AddRing(Vector3 center, Vector3 side, Vector3 normal, float inflation, float ribScale)
        {
            float halfWidth = Mathf.LerpUnclamped(_deflatedRadius, _inflatedRadius, inflation) * ribScale;
            float halfHeight = halfWidth * Mathf.LerpUnclamped(_deflatedSquash, 1f, Mathf.Clamp01(inflation));

            for (int j = 0; j < _sides; j++)
            {
                float angle = j * Mathf.PI * 2f / _sides;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                _vertices.Add(center + side * (cos * halfWidth) + normal * (sin * halfHeight));
                _normals.Add((side * (cos / halfWidth) + normal * (sin / halfHeight)).normalized);
                _uvs.Add(_paletteUv);
            }
        }

        private float GetRibScale(float distanceFromEnd)
        {
            if (_ribDepth <= 0f || _ribSpacing <= 0f)
                return 1f;

            float wave = HALF * (1f + Mathf.Cos(distanceFromEnd / _ribSpacing * Mathf.PI * 2f));
            return 1f + _ribDepth * Mathf.Pow(wave, RIB_SHARPNESS);
        }

        private float GetEndTaper(float distance, float length)
        {
            if (_endTaperLength <= 0f)
                return 1f;

            float distanceToStart = _isStartFitted ? distance : float.MaxValue;
            float distanceToEnd = _isEndFitted ? length - distance : float.MaxValue;
            return Mathf.SmoothStep(0f, 1f, Mathf.Min(distanceToStart, distanceToEnd) / _endTaperLength);
        }

        private void AddCaps()
        {
            int lastRingStart = (_centers.Count - 1) * _sides;

            if (_isStartFitted)
                AddCap(0, -GetTangent(0), false);

            if (_isEndFitted)
                AddCap(lastRingStart, GetTangent(_centers.Count - 1), true);
        }

        private void AddCap(int ringStart, Vector3 outward, bool isEnd)
        {
            int capStart = _vertices.Count;
            Vector3 center = Vector3.zero;

            for (int j = 0; j < _sides; j++)
            {
                Vector3 vertex = _vertices[ringStart + j];
                center += vertex;
                _vertices.Add(vertex);
                _normals.Add(outward);
                _uvs.Add(_paletteUv);
            }

            int centerIndex = _vertices.Count;
            _vertices.Add(center / _sides);
            _normals.Add(outward);
            _uvs.Add(_paletteUv);

            for (int j = 0; j < _sides; j++)
            {
                int current = capStart + j;
                int next = capStart + (j + 1) % _sides;

                _triangles.Add(centerIndex);
                _triangles.Add(isEnd ? current : next);
                _triangles.Add(isEnd ? next : current);
            }
        }

        private void AddTriangles()
        {
            for (int ring = 0; ring < _centers.Count - 1; ring++)
            {
                for (int j = 0; j < _sides; j++)
                {
                    int current = ring * _sides + j;
                    int next = ring * _sides + (j + 1) % _sides;
                    int currentAhead = current + _sides;
                    int nextAhead = next + _sides;

                    _triangles.Add(current);
                    _triangles.Add(next);
                    _triangles.Add(currentAhead);

                    _triangles.Add(next);
                    _triangles.Add(nextAhead);
                    _triangles.Add(currentAhead);
                }
            }
        }

        private void ApplyToMesh()
        {
            Mesh.Clear();
            Mesh.SetVertices(_vertices);
            Mesh.SetNormals(_normals);
            Mesh.SetUVs(0, _uvs);
            Mesh.SetTriangles(_triangles, 0);
            Mesh.RecalculateBounds();
        }

        private Vector3 GetTangent(int index)
        {
            if (_hasStartTangent && index == 0)
                return _startTangent;

            if (_hasEndTangent && index == _centers.Count - 1)
                return _endTangent;

            int previous = Mathf.Max(index - 1, 0);
            int next = Mathf.Min(index + 1, _centers.Count - 1);
            return (_centers[next] - _centers[previous]).normalized;
        }

        private static Vector3 GetInitialNormal(Vector3 tangent)
        {
            Vector3 normal = Vector3.ProjectOnPlane(Vector3.up, tangent);

            if (normal.sqrMagnitude < MIN_NORMAL_SQR_MAGNITUDE)
                normal = Vector3.ProjectOnPlane(Vector3.forward, tangent);

            return normal.normalized;
        }

        private static Vector3 TransportNormal(Vector3 normal, Vector3 fromTangent, Vector3 toTangent)
        {
            Vector3 rotated = Quaternion.FromToRotation(fromTangent, toTangent) * normal;
            Vector3 orthogonal = Vector3.ProjectOnPlane(rotated, toTangent);
            return orthogonal.sqrMagnitude < MIN_NORMAL_SQR_MAGNITUDE ? GetInitialNormal(toTangent) : orthogonal.normalized;
        }

        private static Vector3 GetCatmullRom(Vector3 before, Vector3 from, Vector3 to, Vector3 after, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (2f * from
                + (to - before) * t
                + (2f * before - 5f * from + 4f * to - after) * t2
                + (3f * from - before - 3f * to + after) * t3);
        }
    }
}
