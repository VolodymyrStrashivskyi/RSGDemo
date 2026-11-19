using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class WaterStreamMesh
    {
        private const float FULL_CIRCLE = Mathf.PI * 2f;
        private const int MIN_POINTS = 2;
        private const float VERTICAL_DOT = 0.9f;
        private const float CAP_LENGTH_RATIO = 0.6f;

        private readonly int _sides;
        private readonly Mesh _mesh;
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        public WaterStreamMesh(int sides)
        {
            _sides = sides;
            _mesh = new Mesh { name = "WaterStream" };
            _mesh.MarkDynamic();
        }

        public Mesh Mesh => _mesh;

        public void Build(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii, IReadOnlyList<float> progress, IReadOnlyList<float> opacities)
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();
            _mesh.Clear();

            if (points.Count < MIN_POINTS)
                return;

            Vector3 normal = GetInitialNormal(points[1] - points[0]);
            float distance = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 tangent = GetTangent(points, i);
                normal = Vector3.ProjectOnPlane(normal, tangent).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal);

                if (i > 0)
                    distance += Vector3.Distance(points[i - 1], points[i]);

                AddRing(points[i], normal, binormal, radii[i], distance, new Color(progress[i], 0f, 0f, opacities[i]));
            }

            AddSideTriangles(points.Count);
            AddEndCap(points, radii, progress, opacities);

            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private static Vector3 GetInitialNormal(Vector3 direction)
        {
            Vector3 reference = Mathf.Abs(Vector3.Dot(direction.normalized, Vector3.up)) > VERTICAL_DOT ? Vector3.right : Vector3.up;
            return Vector3.Cross(direction, reference).normalized;
        }

        private static Vector3 GetTangent(IReadOnlyList<Vector3> points, int index)
        {
            Vector3 previous = points[Mathf.Max(0, index - 1)];
            Vector3 next = points[Mathf.Min(points.Count - 1, index + 1)];
            return (next - previous).normalized;
        }

        private void AddRing(Vector3 center, Vector3 normal, Vector3 binormal, float radius, float distance, Color color)
        {
            for (int side = 0; side <= _sides; side++)
            {
                float around = (float)side / _sides;
                float angle = around * FULL_CIRCLE;
                Vector3 direction = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);

                _vertices.Add(center + direction * radius);
                _normals.Add(direction);
                _uvs.Add(new Vector2(distance, around));
                _colors.Add(color);
            }
        }

        private void AddEndCap(IReadOnlyList<Vector3> points, IReadOnlyList<float> radii, IReadOnlyList<float> progress, IReadOnlyList<float> opacities)
        {
            int last = points.Count - 1;
            Vector3 tangent = GetTangent(points, last);
            int ringStart = last * (_sides + 1);
            int tip = _vertices.Count;

            _vertices.Add(points[last] + tangent * (radii[last] * CAP_LENGTH_RATIO));
            _normals.Add(tangent);
            _uvs.Add(new Vector2(_uvs[ringStart].x, 0f));
            _colors.Add(new Color(progress[last], 0f, 0f, opacities[last]));

            for (int side = 0; side < _sides; side++)
            {
                _triangles.Add(ringStart + side);
                _triangles.Add(ringStart + side + 1);
                _triangles.Add(tip);
            }
        }

        private void AddSideTriangles(int ringCount)
        {
            int ringSize = _sides + 1;

            for (int ring = 0; ring < ringCount - 1; ring++)
            {
                for (int side = 0; side < _sides; side++)
                {
                    int current = ring * ringSize + side;
                    int next = current + ringSize;

                    _triangles.Add(current);
                    _triangles.Add(current + 1);
                    _triangles.Add(next);

                    _triangles.Add(current + 1);
                    _triangles.Add(next + 1);
                    _triangles.Add(next);
                }
            }
        }
    }
}
