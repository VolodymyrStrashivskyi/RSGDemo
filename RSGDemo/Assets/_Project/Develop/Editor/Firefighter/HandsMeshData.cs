using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace _Project.Develop.Editor.Firefighter
{
    public class HandsMeshData
    {
        private const int ARMS_SUBMESH = 0;
        private const int GLOVES_SUBMESH = 1;
        private const int SUBMESH_COUNT = 2;

        public readonly List<Vector3> Positions = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> Uvs = new List<Vector2>();
        public readonly List<int> ArmsTriangles = new List<int>();
        public readonly List<int> GloveTriangles = new List<int>();

        private readonly List<byte> _bonesPerVertex = new List<byte>();
        private readonly List<BoneWeight1> _weights = new List<BoneWeight1>();

        public void AddArmsVertices(Mesh arms, int[] order)
        {
            Vector3[] positions = arms.vertices;
            Vector3[] normals = arms.normals;
            Vector2[] uvs = arms.uv;
            NativeArray<byte> bonesPerVertex = arms.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = arms.GetAllBoneWeights();
            int[] offsets = new int[bonesPerVertex.Length];

            for (int vertex = 1; vertex < offsets.Length; vertex++)
                offsets[vertex] = offsets[vertex - 1] + bonesPerVertex[vertex - 1];

            foreach (int source in order)
            {
                Positions.Add(positions[source]);
                Normals.Add(normals[source]);
                Uvs.Add(uvs[source]);
                _bonesPerVertex.Add(bonesPerVertex[source]);

                for (int i = 0; i < bonesPerVertex[source]; i++)
                    _weights.Add(weights[offsets[source] + i]);
            }
        }

        public void AddRigidWeight(int bone)
        {
            _bonesPerVertex.Add(1);
            _weights.Add(new BoneWeight1 { boneIndex = bone, weight = 1f });
        }

        public void WriteTo(Mesh mesh, Matrix4x4[] bindposes)
        {
            mesh.Clear();
            mesh.SetVertices(Positions);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, Uvs);
            mesh.subMeshCount = SUBMESH_COUNT;
            mesh.SetTriangles(ArmsTriangles, ARMS_SUBMESH);
            mesh.SetTriangles(GloveTriangles, GLOVES_SUBMESH);

            using NativeArray<byte> bonesPerVertex = new NativeArray<byte>(_bonesPerVertex.ToArray(), Allocator.Temp);
            using NativeArray<BoneWeight1> weights = new NativeArray<BoneWeight1>(_weights.ToArray(), Allocator.Temp);
            mesh.SetBoneWeights(bonesPerVertex, weights);
            mesh.bindposes = bindposes;
            mesh.RecalculateBounds();
        }
    }
}
