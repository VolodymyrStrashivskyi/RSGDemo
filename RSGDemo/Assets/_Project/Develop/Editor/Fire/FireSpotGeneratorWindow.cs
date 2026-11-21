using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.House;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor.Fire
{
    public class FireSpotGeneratorWindow : EditorWindow
    {
        private const string MENU_PATH = "Tools/Fire/Fire Spot Generator";
        private const string WINDOW_TITLE = "Fire Spot Generator";
        private const string CONFIG_FILTER = "t:FireSpotLayoutConfig";
        private const float MIN_WIDTH = 360f;
        private const float MIN_HEIGHT = 260f;

        [SerializeField] private GameObject _housePrefab;
        [SerializeField] private MeshCollider _structure;
        [SerializeField] private FireSpotLayoutConfig _config;

        private string _report;
        private MessageType _reportType = MessageType.Info;

        [MenuItem(MENU_PATH)]
        private static void Open()
        {
            FireSpotGeneratorWindow window = GetWindow<FireSpotGeneratorWindow>(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
        }

        private void OnEnable()
        {
            if (_config == null)
                _config = FindConfig();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("House", EditorStyles.boldLabel);
            _housePrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("House Prefab", "Prefab with a HouseView on its root"), _housePrefab, typeof(GameObject), false);
            _structure = (MeshCollider)EditorGUILayout.ObjectField(
                new GUIContent("Structure Collider", "Walls, floors and roof; the largest mesh collider when empty"), _structure, typeof(MeshCollider), false);
            _config = (FireSpotLayoutConfig)EditorGUILayout.ObjectField(
                new GUIContent("Layout Config", "Which props burn and how densely the structure is covered"), _config, typeof(FireSpotLayoutConfig), false);

            string problem = FindProblem();

            if (problem != null)
                EditorGUILayout.HelpBox(problem, MessageType.Warning);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(problem != null))
            {
                if (GUILayout.Button("Generate"))
                    Generate();

                if (GUILayout.Button("Clear Generated"))
                    ClearGenerated();
            }

            if (string.IsNullOrEmpty(_report) == false)
                EditorGUILayout.HelpBox(_report, _reportType);
        }

        private string FindProblem()
        {
            if (_housePrefab == null)
                return "Pick a house prefab.";

            if (PrefabUtility.IsPartOfPrefabAsset(_housePrefab) == false)
                return "The house has to be a prefab asset, not a scene object.";

            if (_housePrefab.GetComponent<HouseView>() == null)
                return "The prefab root has no HouseView.";

            if (_structure != null && _structure.transform.IsChildOf(_housePrefab.transform) == false)
                return "The structure collider is not part of this prefab.";

            if (_config == null)
                return "Pick a layout config.";

            if (_config.StructureMaterial == null)
                return "The layout config has no structure material.";

            return null;
        }

        private void Generate()
        {
            string path = AssetDatabase.GetAssetPath(_housePrefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                MeshCollider structure = ResolveStructure(root);

                if (structure == null)
                {
                    ShowReport("No mesh collider for walls, floors and roof found in the prefab.", MessageType.Error);
                    return;
                }

                FireSpotLayoutReport report = FireSpotLayoutBuilder.Generate(root, structure, _config);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                ShowReport(report.ToString(), report.HasDuplicates ? MessageType.Warning : MessageType.Info);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void ClearGenerated()
        {
            string path = AssetDatabase.GetAssetPath(_housePrefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                FireSpotLayoutBuilder.ClearGenerated(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                ShowReport("Generated structure spots removed; markers on props stay.", MessageType.Info);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private MeshCollider ResolveStructure(GameObject root)
        {
            if (_structure == null)
                return FireSpotLayoutBuilder.FindStructure(root);

            string relativePath = AnimationUtility.CalculateTransformPath(_structure.transform, _housePrefab.transform);
            Transform match = string.IsNullOrEmpty(relativePath) ? root.transform : root.transform.Find(relativePath);
            return match != null ? match.GetComponent<MeshCollider>() : null;
        }

        private void ShowReport(string report, MessageType type)
        {
            _report = report;
            _reportType = type;
            Debug.Log($"[FireSpotGenerator] {report}");
        }

        private static FireSpotLayoutConfig FindConfig()
        {
            string[] guids = AssetDatabase.FindAssets(CONFIG_FILTER);
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<FireSpotLayoutConfig>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }
    }
}
