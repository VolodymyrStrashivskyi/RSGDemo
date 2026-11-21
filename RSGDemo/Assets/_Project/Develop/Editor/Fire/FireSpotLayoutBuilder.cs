using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Fire;
using _Project.Develop.Runtime.Gameplay.House;
using UnityEditor;
using UnityEngine;

namespace _Project.Develop.Editor.Fire
{
    public static class FireSpotLayoutBuilder
    {
        private const string SPOTS_NAME = "FireSpots";
        private const string GENERATED_NAME = "Generated";
        private const string WALL_PREFIX = "Wall_";
        private const string FACADE_PREFIX = "Facade_";
        private const string ROOF_PREFIX = "Roof_";

        private const float GRID_STEP = 0.5f;
        private const float HALF = 0.5f;
        private const float STOREY_PROBE_HEIGHT = 1.2f;
        private const float MIN_STOREY_HEIGHT = 2f;
        private const float MAX_STOREY_HEIGHT = 3.6f;
        private const float FLOOR_NORMAL_MIN = 0.7f;
        private const float WALL_PROBE_HEIGHT = 1f;
        private const float WALL_PROBE_DISTANCE = 0.8f;
        private const float WALL_NORMAL_MAX = 0.3f;
        private const float WALL_SPOT_OFFSET = 0.35f;
        private const float CEILING_GAP = 0.1f;
        private const float FACADE_PROBE_MARGIN = 1.5f;
        private const float FACADE_SPOT_OFFSET = 0.3f;
        private const float FACADE_FACING_MIN = 0.7f;
        private const float ROOF_MIN_HEIGHT = 2f;
        private const float ROOF_NORMAL_MIN = 0.3f;
        private const float ROOF_PROBE_MARGIN = 1f;

        private static readonly Vector3[] _horizontalDirections =
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right
        };

        public static MeshCollider FindStructure(GameObject root)
        {
            MeshCollider largest = null;
            float largestVolume = 0f;

            foreach (MeshCollider candidate in root.GetComponentsInChildren<MeshCollider>(true))
            {
                Vector3 size = candidate.sharedMesh != null ? candidate.sharedMesh.bounds.size : Vector3.zero;
                float volume = size.x * size.y * size.z;

                if (volume <= largestVolume)
                    continue;

                largest = candidate;
                largestVolume = volume;
            }

            return largest;
        }

        public static FireSpotLayoutReport Generate(GameObject root, MeshCollider structure, FireSpotLayoutConfig config)
        {
            FireSpotLayoutReport report = new FireSpotLayoutReport();
            Transform spotsRoot = FindOrCreateChild(root.transform, SPOTS_NAME);
            Transform generated = RecreateGeneratedRoot(spotsRoot);

            AddPropMarkers(root, structure, spotsRoot, config, report);

            Bounds bounds = structure.bounds;
            List<Storey> storeys = FindStoreys(structure, bounds);
            List<Vector3> manualStructure = CollectManualStructurePositions(spotsRoot);
            report.Storeys = storeys.Count;

            if (config.PlaceWalls)
                report.Walls = AddWallSpots(structure, bounds, storeys, generated, config, new List<Vector3>(manualStructure));

            if (config.PlaceFacades)
                report.Facades = AddFacadeSpots(structure, bounds, storeys, generated, config, new List<Vector3>(manualStructure));

            if (config.PlaceRoof)
                report.Roofs = AddRoofSpots(structure, bounds, generated, config, new List<Vector3>(manualStructure));

            List<FireSpotMarker> markers = new List<FireSpotMarker>(root.GetComponentsInChildren<FireSpotMarker>(true));
            SetHouseSpots(root, markers);
            report.Total = markers.Count;
            report.DuplicateIds = FindDuplicateIds(markers);
            return report;
        }

        public static void ClearGenerated(GameObject root)
        {
            Transform spotsRoot = root.transform.Find(SPOTS_NAME);

            if (spotsRoot != null)
                RecreateGeneratedRoot(spotsRoot);

            SetHouseSpots(root, new List<FireSpotMarker>(root.GetComponentsInChildren<FireSpotMarker>(true)));
        }

        private static Transform FindOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);

            if (child != null)
                return child;

            GameObject created = new GameObject(childName);
            created.transform.SetParent(parent, false);
            return created.transform;
        }

        private static Transform RecreateGeneratedRoot(Transform spotsRoot)
        {
            Transform old = spotsRoot.Find(GENERATED_NAME);

            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            return FindOrCreateChild(spotsRoot, GENERATED_NAME);
        }

        private static void AddPropMarkers(GameObject root, MeshCollider structure, Transform spotsRoot,
            FireSpotLayoutConfig config, FireSpotLayoutReport report)
        {
            foreach (MeshRenderer mesh in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Transform prop = mesh.transform;

                if (prop == structure.transform || prop.IsChildOf(spotsRoot))
                    continue;

                if (HasMarkerAbove(prop, root.transform))
                {
                    if (prop.GetComponent<FireSpotMarker>() != null)
                        report.PropsKept++;

                    continue;
                }

                FireMaterialConfig material = ChooseMaterial(prop.name, config);

                if (material == null)
                    continue;

                Bounds bounds = mesh.bounds;
                float halfWidth = (bounds.size.x + bounds.size.z) * HALF * HALF;
                float radius = Mathf.Clamp(halfWidth * config.PropRadiusShare, config.PropMinRadius, config.PropMaxRadius);
                float height = Mathf.Clamp(bounds.size.y, config.PropMinHeight, config.PropMaxHeight);
                prop.gameObject.AddComponent<FireSpotMarker>().Setup(material, radius, height, true);
                report.PropsAdded++;
            }
        }

        private static bool HasMarkerAbove(Transform prop, Transform root)
        {
            for (Transform current = prop; current != null && current != root; current = current.parent)
            {
                if (current.GetComponent<FireSpotMarker>() != null)
                    return true;
            }

            return false;
        }

        private static FireMaterialConfig ChooseMaterial(string objectName, FireSpotLayoutConfig config)
        {
            foreach (FireMaterialRule rule in config.PropRules)
            {
                if (string.IsNullOrEmpty(rule.NamePrefix) == false && objectName.StartsWith(rule.NamePrefix))
                    return rule.Material;
            }

            return null;
        }

        private static List<Vector3> CollectManualStructurePositions(Transform spotsRoot)
        {
            List<Vector3> positions = new List<Vector3>();

            foreach (Transform child in spotsRoot)
            {
                if (child.TryGetComponent(out FireSpotMarker marker) && marker.BurnsOwnMeshes == false)
                    positions.Add(marker.transform.position);
            }

            return positions;
        }

        private static List<Storey> FindStoreys(MeshCollider structure, Bounds bounds)
        {
            List<Storey> storeys = new List<Storey>();
            float probeHeight = bounds.min.y + STOREY_PROBE_HEIGHT;

            while (probeHeight < bounds.max.y)
            {
                if (TryFindStoreyAt(structure, bounds, probeHeight, out Storey storey))
                {
                    storeys.Add(storey);
                    probeHeight = storey.Ceiling + STOREY_PROBE_HEIGHT;
                }
                else
                {
                    probeHeight += GRID_STEP;
                }
            }

            return storeys;
        }

        private static bool TryFindStoreyAt(MeshCollider structure, Bounds bounds, float probeHeight, out Storey storey)
        {
            for (float x = bounds.min.x; x <= bounds.max.x; x += GRID_STEP)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += GRID_STEP)
                {
                    if (TryFindStorey(structure, new Vector3(x, probeHeight, z), out storey))
                        return true;
                }
            }

            storey = default;
            return false;
        }

        private static bool TryFindStorey(MeshCollider structure, Vector3 probe, out Storey storey)
        {
            storey = default;

            if (structure.Raycast(new Ray(probe, Vector3.down), out RaycastHit floor, MAX_STOREY_HEIGHT) == false)
                return false;

            if (floor.normal.y < FLOOR_NORMAL_MIN)
                return false;

            if (structure.Raycast(new Ray(probe, Vector3.up), out RaycastHit ceiling, MAX_STOREY_HEIGHT) == false)
                return false;

            float height = ceiling.point.y - floor.point.y;

            if (height < MIN_STOREY_HEIGHT || height > MAX_STOREY_HEIGHT)
                return false;

            storey = new Storey(floor.point.y, ceiling.point.y);
            return true;
        }

        private static int AddWallSpots(MeshCollider structure, Bounds bounds, List<Storey> storeys, Transform generated,
            FireSpotLayoutConfig config, List<Vector3> taken)
        {
            int total = 0;

            for (int storeyIndex = 0; storeyIndex < storeys.Count; storeyIndex++)
            {
                Storey storey = storeys[storeyIndex];
                int count = 0;

                for (float x = bounds.min.x; x <= bounds.max.x; x += GRID_STEP)
                {
                    for (float z = bounds.min.z; z <= bounds.max.z; z += GRID_STEP)
                    {
                        Vector3 probe = new Vector3(x, storey.Floor + WALL_PROBE_HEIGHT, z);

                        if (IsInsideStorey(structure, probe, storey) == false)
                            continue;

                        foreach (Vector3 direction in _horizontalDirections)
                        {
                            if (structure.Raycast(new Ray(probe, direction), out RaycastHit wall, WALL_PROBE_DISTANCE) == false)
                                continue;

                            if (Mathf.Abs(wall.normal.y) > WALL_NORMAL_MAX)
                                continue;

                            Vector3 spot = wall.point + wall.normal * WALL_SPOT_OFFSET;
                            spot.y = storey.Floor;

                            if (IsFarFromAll(spot, taken, config.WallSpacing) == false)
                                continue;

                            taken.Add(spot);
                            string spotName = $"{WALL_PREFIX}F{storeyIndex + 1}_{count++}";
                            CreateMarker(generated, spotName, spot, config.WallRadius, storey.Height - CEILING_GAP, config.StructureMaterial);
                            total++;
                        }
                    }
                }
            }

            return total;
        }

        private static bool IsInsideStorey(MeshCollider structure, Vector3 probe, Storey storey)
        {
            if (structure.Raycast(new Ray(probe, Vector3.down), out RaycastHit floor, WALL_PROBE_HEIGHT + GRID_STEP) == false)
                return false;

            if (Mathf.Abs(floor.point.y - storey.Floor) > GRID_STEP)
                return false;

            return structure.Raycast(new Ray(probe, Vector3.up), out RaycastHit _, storey.Height);
        }

        private static int AddFacadeSpots(MeshCollider structure, Bounds bounds, List<Storey> storeys, Transform generated,
            FireSpotLayoutConfig config, List<Vector3> taken)
        {
            float probeDistance = Mathf.Max(bounds.size.x, bounds.size.z);
            int total = 0;

            for (int storeyIndex = 0; storeyIndex < storeys.Count; storeyIndex++)
            {
                Storey storey = storeys[storeyIndex];
                float probeHeight = storey.Floor + storey.Height * HALF;
                float baseHeight = storeyIndex == 0 ? bounds.min.y : storey.Floor;
                float spotHeight = storey.Ceiling - baseHeight;
                int count = 0;

                foreach (Vector3 direction in _horizontalDirections)
                {
                    Vector3 across = Vector3.Cross(Vector3.up, direction);
                    Vector3 start = bounds.center - direction * (Vector3.Scale(bounds.extents, direction).magnitude + FACADE_PROBE_MARGIN);
                    float halfWidth = Vector3.Scale(bounds.extents, across).magnitude;

                    for (float offset = -halfWidth; offset <= halfWidth; offset += GRID_STEP)
                    {
                        Vector3 origin = start + across * offset;
                        origin.y = probeHeight;

                        if (structure.Raycast(new Ray(origin, direction), out RaycastHit wall, probeDistance) == false)
                            continue;

                        if (Vector3.Dot(wall.normal, -direction) < FACADE_FACING_MIN)
                            continue;

                        Vector3 spot = wall.point + wall.normal * FACADE_SPOT_OFFSET;
                        spot.y = baseHeight;

                        if (IsFarFromAll(spot, taken, config.FacadeSpacing) == false)
                            continue;

                        taken.Add(spot);
                        string spotName = $"{FACADE_PREFIX}F{storeyIndex + 1}_{count++}";
                        CreateMarker(generated, spotName, spot, config.FacadeRadius, spotHeight, config.StructureMaterial);
                        total++;
                    }
                }
            }

            return total;
        }

        private static int AddRoofSpots(MeshCollider structure, Bounds bounds, Transform generated,
            FireSpotLayoutConfig config, List<Vector3> taken)
        {
            float top = bounds.max.y + ROOF_PROBE_MARGIN;
            float probeDistance = bounds.size.y + ROOF_PROBE_MARGIN;
            int count = 0;

            for (float x = bounds.min.x; x <= bounds.max.x; x += GRID_STEP)
            {
                for (float z = bounds.min.z; z <= bounds.max.z; z += GRID_STEP)
                {
                    Ray ray = new Ray(new Vector3(x, top, z), Vector3.down);

                    if (structure.Raycast(ray, out RaycastHit roof, probeDistance) == false)
                        continue;

                    if (roof.point.y - bounds.min.y < ROOF_MIN_HEIGHT || roof.normal.y < ROOF_NORMAL_MIN)
                        continue;

                    if (IsFarFromAll(roof.point, taken, config.RoofSpacing) == false)
                        continue;

                    taken.Add(roof.point);
                    CreateMarker(generated, $"{ROOF_PREFIX}{count++}", roof.point, config.RoofRadius, config.RoofFlameHeight, config.StructureMaterial);
                }
            }

            return count;
        }

        private static bool IsFarFromAll(Vector3 point, List<Vector3> taken, float spacing)
        {
            foreach (Vector3 other in taken)
            {
                if (Vector3.Distance(point, other) < spacing)
                    return false;
            }

            return true;
        }

        private static void CreateMarker(Transform parent, string markerName, Vector3 position, float radius,
            float height, FireMaterialConfig material)
        {
            GameObject markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.position = position;
            markerObject.AddComponent<FireSpotMarker>().Setup(material, radius, height, false);
        }

        private static void SetHouseSpots(GameObject root, List<FireSpotMarker> markers)
        {
            SerializedObject serialized = new SerializedObject(root.GetComponent<HouseView>());
            SerializedProperty property = serialized.FindProperty("_fireSpots");
            property.arraySize = markers.Count;

            for (int i = 0; i < markers.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<string> FindDuplicateIds(List<FireSpotMarker> markers)
        {
            HashSet<string> seen = new HashSet<string>();
            List<string> duplicates = new List<string>();

            foreach (FireSpotMarker marker in markers)
            {
                if (seen.Add(marker.Id) == false && duplicates.Contains(marker.Id) == false)
                    duplicates.Add(marker.Id);
            }

            return duplicates;
        }

        private readonly struct Storey
        {
            public readonly float Floor;
            public readonly float Ceiling;

            public Storey(float floor, float ceiling)
            {
                Floor = floor;
                Ceiling = ceiling;
            }

            public float Height => Ceiling - Floor;
        }
    }
}
