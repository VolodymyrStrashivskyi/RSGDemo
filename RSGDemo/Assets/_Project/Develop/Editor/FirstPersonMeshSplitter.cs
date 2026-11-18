using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor
{
    // Від першої особи камера бачить лише руки, а решта тіла лежить на шарі, який вона не рендерить.
    // Модель — один скінований меш, тому ріжемо його за кістками на два меші з тим самим скелетом.
    // Запускати знову після кожного переімпорту Firefighter.fbx.
    public static class FirstPersonMeshSplitter
    {
        private const string MENU_PATH = "Tools/Firefighter/Split First Person Mesh";
        private const string SOURCE_PREFAB_PATH = "Assets/_Project/Prefabs/Firefighter.prefab";
        private const string OUTPUT_FOLDER = "Assets/_Project/Art/Models/Firefighter";
        private const string HANDS_MESH_NAME = "Firefighter_Hands";
        private const string HIDDEN_BODY_MESH_NAME = "Firefighter_HiddenBody";

        private static readonly HashSet<string> _handBoneNames = new HashSet<string>
        {
            "UpperArm.L", "LowerArm.L", "Hand.L", "UpperArm.R", "LowerArm.R", "Hand.R"
        };

        [MenuItem(MENU_PATH)]
        private static void Split()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SOURCE_PREFAB_PATH);
            SkinnedMeshRenderer renderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh source = renderer.sharedMesh;

            bool[] isHandVertex = FindHandVertices(source, renderer.bones);
            int[] triangles = source.triangles;
            List<int> handTriangles = new List<int>();
            List<int> bodyTriangles = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                bool isHandTriangle = isHandVertex[triangles[i]]
                    && isHandVertex[triangles[i + 1]]
                    && isHandVertex[triangles[i + 2]];

                List<int> target = isHandTriangle ? handTriangles : bodyTriangles;
                target.Add(triangles[i]);
                target.Add(triangles[i + 1]);
                target.Add(triangles[i + 2]);
            }

            FillSubMesh(source, handTriangles, LoadOrCreateMesh(HANDS_MESH_NAME));
            FillSubMesh(source, bodyTriangles, LoadOrCreateMesh(HIDDEN_BODY_MESH_NAME));
            AssetDatabase.SaveAssets();

            int handIndexCount = handTriangles.Count;
            int bodyIndexCount = bodyTriangles.Count;
            Debug.Log($"[FirstPersonMeshSplitter] Hands indices: {handIndexCount}, body indices: {bodyIndexCount}");
        }

        private static bool[] FindHandVertices(Mesh mesh, Transform[] bones)
        {
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            bool[] isHandVertex = new bool[mesh.vertexCount];
            int weightIndex = 0;

            for (int vertex = 0; vertex < bonesPerVertex.Length; vertex++)
            {
                // Ваги відсортовані за спаданням: перша — домінантна кістка вершини.
                if (bonesPerVertex[vertex] > 0)
                    isHandVertex[vertex] = _handBoneNames.Contains(bones[weights[weightIndex].boneIndex].name);

                weightIndex += bonesPerVertex[vertex];
            }

            return isHandVertex;
        }

        private static void FillSubMesh(Mesh source, List<int> sourceTriangles, Mesh mesh)
        {
            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<int> triangles = new List<int>(sourceTriangles.Count);

            foreach (int sourceIndex in sourceTriangles)
            {
                if (remap.TryGetValue(sourceIndex, out int index) == false)
                {
                    index = remap.Count;
                    remap.Add(sourceIndex, index);
                }

                triangles.Add(index);
            }

            int[] order = new int[remap.Count];

            foreach (KeyValuePair<int, int> pair in remap)
                order[pair.Value] = pair.Key;

            mesh.Clear();
            mesh.SetVertices(Pick(source.vertices, order));
            mesh.SetNormals(Pick(source.normals, order));
            mesh.SetTangents(Pick(source.tangents, order));
            mesh.SetUVs(0, Pick(source.uv, order));

            if (source.colors.Length > 0)
                mesh.SetColors(Pick(source.colors, order));

            mesh.SetTriangles(triangles, 0);
            CopyBoneWeights(source, mesh, order);
            mesh.bindposes = source.bindposes;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
        }

        private static void CopyBoneWeights(Mesh source, Mesh target, int[] order)
        {
            NativeArray<byte> sourceBonesPerVertex = source.GetBonesPerVertex();
            NativeArray<BoneWeight1> sourceWeights = source.GetAllBoneWeights();
            int[] offsets = new int[sourceBonesPerVertex.Length];

            for (int vertex = 1; vertex < offsets.Length; vertex++)
                offsets[vertex] = offsets[vertex - 1] + sourceBonesPerVertex[vertex - 1];

            List<byte> bonesPerVertex = new List<byte>(order.Length);
            List<BoneWeight1> weights = new List<BoneWeight1>();

            foreach (int sourceVertex in order)
            {
                byte count = sourceBonesPerVertex[sourceVertex];
                bonesPerVertex.Add(count);

                for (int i = 0; i < count; i++)
                    weights.Add(sourceWeights[offsets[sourceVertex] + i]);
            }

            using NativeArray<byte> bonesPerVertexArray = new NativeArray<byte>(bonesPerVertex.ToArray(), Allocator.Temp);
            using NativeArray<BoneWeight1> weightsArray = new NativeArray<BoneWeight1>(weights.ToArray(), Allocator.Temp);
            target.SetBoneWeights(bonesPerVertexArray, weightsArray);
        }

        private static T[] Pick<T>(T[] source, int[] order)
        {
            T[] result = new T[order.Length];

            for (int i = 0; i < order.Length; i++)
                result[i] = source[order[i]];

            return result;
        }

        // Існуючий ассет заповнюємо на місці: GUID лишається, посилання з префаба не ламаються.
        private static Mesh LoadOrCreateMesh(string name)
        {
            string path = $"{OUTPUT_FOLDER}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh != null)
                return mesh;

            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}
