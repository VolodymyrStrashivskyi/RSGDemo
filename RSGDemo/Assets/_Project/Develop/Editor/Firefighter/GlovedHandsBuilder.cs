using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor.Firefighter
{
    public static class GlovedHandsBuilder
    {
        private const string MENU_PATH = "Tools/Firefighter/Build Gloved Hands";
        private const string RIG_PREFAB_PATH = "Assets/_Project/Prefabs/Firefighter.prefab";
        private const string ARMS_MESH_PATH = "Assets/_Project/Art/Models/Firefighter/Firefighter_Hands.asset";
        private const string GLOVES_MODEL_PATH = "Assets/_Project/Art/Models/Firefighter/Gloves.fbx";
        private const string OUTPUT_MESH_PATH = "Assets/_Project/Art/Models/Firefighter/Firefighter_GlovedHands.asset";
        private const string OUTPUT_MESH_NAME = "Firefighter_GlovedHands";
        private const string LEFT_HAND_BONE = "Hand.L";
        private const string RIGHT_HAND_BONE = "Hand.R";
        private const string LEFT_GLOVE_MESH = "Glove_L";
        private const string RIGHT_GLOVE_MESH = "Glove_R";
        private const float BONE_UNITS_PER_METER = 0.01f;
        private const float GLOVE_SCALE = 1.15f;
        private const int TRIANGLE_CORNERS = 3;
        private const float GLOVE_GREEN_DOMINANCE = 1.15f;
        private const int DECODE_TEXTURE_SIZE = 2;

        [MenuItem(MENU_PATH)]
        private static void Build()
        {
            GameObject rig = AssetDatabase.LoadAssetAtPath<GameObject>(RIG_PREFAB_PATH);
            SkinnedMeshRenderer rigRenderer = rig.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Transform[] bones = rigRenderer.bones;
            Texture2D albedo = ReadAlbedo(rigRenderer.sharedMaterial.mainTexture);
            Mesh arms = AssetDatabase.LoadAssetAtPath<Mesh>(ARMS_MESH_PATH);
            int leftHand = FindBone(bones, LEFT_HAND_BONE);
            int rightHand = FindBone(bones, RIGHT_HAND_BONE);

            HandsMeshData data = new HandsMeshData();
            int[] remap = CopyArmsWithoutGloves(arms, albedo, leftHand, rightHand, data);
            AddGlove(LoadGlove(LEFT_GLOVE_MESH), arms.bindposes[leftHand], leftHand, data);
            AddGlove(LoadGlove(RIGHT_GLOVE_MESH), arms.bindposes[rightHand], rightHand, data);

            Mesh mesh = LoadOrCreateMesh();
            data.WriteTo(mesh, arms.bindposes);
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            Object.DestroyImmediate(albedo);
            Debug.Log($"[GlovedHandsBuilder] Arms vertices kept: {remap.Length}, total vertices: {mesh.vertexCount}");
        }

        private static int[] CopyArmsWithoutGloves(Mesh arms, Texture2D albedo, int leftHand, int rightHand, HandsMeshData data)
        {
            int[] dominantBones = FindDominantBones(arms);
            int[] triangles = arms.triangles;
            Vector2[] uvs = arms.uv;
            Dictionary<int, int> remap = new Dictionary<int, int>();

            for (int i = 0; i < triangles.Length; i += TRIANGLE_CORNERS)
            {
                if (IsGloveTriangle(triangles, i, dominantBones, leftHand, rightHand) || IsGloveColored(triangles, i, uvs, albedo))
                    continue;

                for (int corner = 0; corner < TRIANGLE_CORNERS; corner++)
                    data.ArmsTriangles.Add(Remap(triangles[i + corner], remap));
            }

            int[] order = new int[remap.Count];

            foreach (KeyValuePair<int, int> pair in remap)
                order[pair.Value] = pair.Key;

            data.AddArmsVertices(arms, order);
            return order;
        }

        private static void AddGlove(Mesh glove, Matrix4x4 bindpose, int handBone, HandsMeshData data)
        {
            Matrix4x4 boneToMesh = bindpose.inverse;
            float scale = BONE_UNITS_PER_METER * GLOVE_SCALE;
            int first = data.Positions.Count;
            Vector3[] positions = glove.vertices;
            Vector3[] normals = glove.normals;
            Vector2[] uvs = glove.uv;

            for (int i = 0; i < positions.Length; i++)
            {
                data.Positions.Add(boneToMesh.MultiplyPoint3x4(positions[i] * scale));
                data.Normals.Add(boneToMesh.MultiplyVector(normals[i]).normalized);
                data.Uvs.Add(uvs[i]);
                data.AddRigidWeight(handBone);
            }

            foreach (int index in glove.triangles)
                data.GloveTriangles.Add(first + index);
        }

        private static int[] FindDominantBones(Mesh mesh)
        {
            NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> weights = mesh.GetAllBoneWeights();
            int[] dominant = new int[mesh.vertexCount];
            int weightIndex = 0;

            for (int vertex = 0; vertex < bonesPerVertex.Length; vertex++)
            {
                float bestWeight = -1f;

                for (int i = 0; i < bonesPerVertex[vertex]; i++)
                {
                    BoneWeight1 weight = weights[weightIndex + i];

                    if (weight.weight > bestWeight)
                    {
                        bestWeight = weight.weight;
                        dominant[vertex] = weight.boneIndex;
                    }
                }

                weightIndex += bonesPerVertex[vertex];
            }

            return dominant;
        }

        private static bool IsGloveTriangle(int[] triangles, int start, int[] dominantBones, int leftHand, int rightHand)
        {
            for (int corner = 0; corner < TRIANGLE_CORNERS; corner++)
            {
                int bone = dominantBones[triangles[start + corner]];

                if (bone == leftHand || bone == rightHand)
                    return true;
            }

            return false;
        }

        private static bool IsGloveColored(int[] triangles, int start, Vector2[] uvs, Texture2D albedo)
        {
            Vector2 centre = (uvs[triangles[start]] + uvs[triangles[start + 1]] + uvs[triangles[start + 2]]) / TRIANGLE_CORNERS;
            Color color = albedo.GetPixelBilinear(centre.x, centre.y);
            return color.g > color.r * GLOVE_GREEN_DOMINANCE && color.g > color.b * GLOVE_GREEN_DOMINANCE;
        }

        private static Texture2D ReadAlbedo(Texture texture)
        {
            Texture2D decoded = new Texture2D(DECODE_TEXTURE_SIZE, DECODE_TEXTURE_SIZE);
            decoded.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)));
            return decoded;
        }

        private static int Remap(int sourceIndex, Dictionary<int, int> remap)
        {
            if (remap.TryGetValue(sourceIndex, out int index))
                return index;

            index = remap.Count;
            remap.Add(sourceIndex, index);
            return index;
        }

        private static int FindBone(Transform[] bones, string boneName)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i].name == boneName)
                    return i;
            }

            throw new KeyNotFoundException($"Bone '{boneName}' not found on {RIG_PREFAB_PATH}");
        }

        private static Mesh LoadGlove(string meshName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(GLOVES_MODEL_PATH))
            {
                if (asset is Mesh mesh && mesh.name == meshName)
                    return mesh;
            }

            throw new KeyNotFoundException($"Mesh '{meshName}' not found in {GLOVES_MODEL_PATH}");
        }

        private static Mesh LoadOrCreateMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(OUTPUT_MESH_PATH);

            if (mesh != null)
                return mesh;

            mesh = new Mesh { name = OUTPUT_MESH_NAME };
            AssetDatabase.CreateAsset(mesh, OUTPUT_MESH_PATH);
            return mesh;
        }
    }
}
