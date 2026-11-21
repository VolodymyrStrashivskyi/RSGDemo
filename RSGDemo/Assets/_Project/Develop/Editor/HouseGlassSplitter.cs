using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor
{
    public static class HouseGlassSplitter
    {
        private const string MENU_PATH = "Tools/House/Split Window Glass";
        private const string HOUSE_PREFAB_PATH = "Assets/_Project/Prefabs/HouseInterior.prefab";
        private const string SPLIT_MESH_PATH = "Assets/_Project/Art/Models/Street/HouseInterior_Shell_Split.asset";
        private const string GLASS_MATERIAL_PATH = "Assets/_Project/Art/Materials/House_Glass_Mat.mat";
        private const string WALL_MATERIAL_PATH = "Assets/_Project/Art/Materials/House_Burnable_Mat.mat";
        private const string PALETTE_PATH = "Assets/_Project/Art/Textures/Palette/Palette.png";
        private const string SHELL_NAME = "HouseInterior_Shell";
        private const string SPLIT_MESH_NAME = "HouseInterior_Shell_Split";
        private const string LIT_SHADER = "Universal Render Pipeline/Lit";
        private const string SHADOW_CASTER_PASS = "ShadowCaster";
        private const string TRANSPARENT_KEYWORD = "_SURFACE_TYPE_TRANSPARENT";
        private const string RENDER_TYPE_TAG = "RenderType";
        private const string TRANSPARENT_TAG = "Transparent";
        private const float COLOR_TOLERANCE = 0.02f;
        private const float GLASS_SMOOTHNESS = 0.95f;
        private const int VERTICES_PER_TRIANGLE = 3;
        private const int WALL_SUBMESH = 0;
        private const int GLASS_SUBMESH = 1;

        private static readonly Color _glassPaletteColor = new Color32(0x4F, 0x54, 0x5D, 0xFF);
        private static readonly Color _glassTint = new Color(0.62f, 0.76f, 0.84f, 0.22f);

        [MenuItem(MENU_PATH)]
        private static void Split()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HOUSE_PREFAB_PATH);

            try
            {
                Transform shell = root.transform.Find(SHELL_NAME);
                Mesh source = shell.GetComponent<MeshCollider>().sharedMesh;
                Mesh split = BuildSplitMesh(source, LoadPalette(), out int glassTriangles);

                shell.GetComponent<MeshFilter>().sharedMesh = split;
                shell.GetComponent<MeshRenderer>().sharedMaterials = new[]
                {
                    AssetDatabase.LoadAssetAtPath<Material>(WALL_MATERIAL_PATH),
                    LoadOrCreateGlassMaterial()
                };

                PrefabUtility.SaveAsPrefabAsset(root, HOUSE_PREFAB_PATH);
                Debug.Log($"[HouseGlassSplitter] Glass triangles: {glassTriangles}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Texture2D LoadPalette()
        {
            Texture2D palette = new Texture2D(1, 1);
            palette.LoadImage(System.IO.File.ReadAllBytes(PALETTE_PATH));
            return palette;
        }

        private static Mesh BuildSplitMesh(Mesh source, Texture2D palette, out int glassTriangles)
        {
            Vector2[] uvs = source.uv;
            int[] triangles = source.triangles;
            List<int> walls = new List<int>();
            List<int> glass = new List<int>();

            for (int i = 0; i < triangles.Length; i += VERTICES_PER_TRIANGLE)
            {
                Vector2 uv = (uvs[triangles[i]] + uvs[triangles[i + 1]] + uvs[triangles[i + 2]]) / VERTICES_PER_TRIANGLE;
                List<int> target = IsGlass(palette.GetPixelBilinear(uv.x, uv.y)) ? glass : walls;
                target.Add(triangles[i]);
                target.Add(triangles[i + 1]);
                target.Add(triangles[i + 2]);
            }

            Mesh split = LoadOrCreateMesh();
            split.Clear();
            split.name = SPLIT_MESH_NAME;
            split.indexFormat = source.indexFormat;
            split.vertices = source.vertices;
            split.normals = source.normals;
            split.tangents = source.tangents;
            split.uv = uvs;
            split.subMeshCount = GLASS_SUBMESH + 1;
            split.SetTriangles(walls, WALL_SUBMESH);
            split.SetTriangles(glass, GLASS_SUBMESH);
            split.RecalculateBounds();
            EditorUtility.SetDirty(split);
            AssetDatabase.SaveAssets();

            glassTriangles = glass.Count / VERTICES_PER_TRIANGLE;
            return split;
        }

        private static bool IsGlass(Color color) =>
            Mathf.Abs(color.r - _glassPaletteColor.r) < COLOR_TOLERANCE
            && Mathf.Abs(color.g - _glassPaletteColor.g) < COLOR_TOLERANCE
            && Mathf.Abs(color.b - _glassPaletteColor.b) < COLOR_TOLERANCE;

        private static Mesh LoadOrCreateMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SPLIT_MESH_PATH);

            if (mesh != null)
                return mesh;

            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, SPLIT_MESH_PATH);
            return mesh;
        }

        private static Material LoadOrCreateGlassMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GLASS_MATERIAL_PATH);

            if (material == null)
            {
                material = new Material(Shader.Find(LIT_SHADER));
                AssetDatabase.CreateAsset(material, GLASS_MATERIAL_PATH);
            }

            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Smoothness", GLASS_SMOOTHNESS);
            material.SetColor("_BaseColor", _glassTint);
            material.EnableKeyword(TRANSPARENT_KEYWORD);
            material.SetOverrideTag(RENDER_TYPE_TAG, TRANSPARENT_TAG);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetShaderPassEnabled(SHADOW_CASTER_PASS, false);
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
