using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSpotMarker : MonoBehaviour
    {
        private const int GIZMO_SEGMENTS = 16;
        private const float FULL_TURN = Mathf.PI * 2f;

        private static readonly Color _gizmoColor = new Color(1f, 0.45f, 0.1f);

        [SerializeField, Tooltip("What burns here")]
        private FireMaterialConfig _material;
        [SerializeField, Min(0.05f), Tooltip("Radius of the burning area, m")]
        private float _radius = 0.5f;
        [SerializeField, Min(0.1f), Tooltip("Height of the flames above the bottom of the spot, m")]
        private float _height = 1.2f;
        [SerializeField, Tooltip("This object itself burns: the spot sits under its meshes, chars them and moves with them")]
        private bool _burnsOwnMeshes;

        private MeshRenderer[] _ownMeshes;

        public string Id => name;

        public FireMaterialConfig Material => _material;

        public float Radius => _radius;

        public float Height => _height;

        public bool BurnsOwnMeshes => _burnsOwnMeshes;

        public bool IsMovable => _burnsOwnMeshes && GetComponentInParent<Rigidbody>(true) != null;

        public MeshRenderer[] CharredMeshes => _burnsOwnMeshes ? GetOwnMeshes() : new MeshRenderer[0];

        public Vector3 SpotPosition
        {
            get
            {
                if (_burnsOwnMeshes == false || TryGetMeshBounds(out Bounds bounds) == false)
                    return transform.position;

                return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            }
        }

        public void Setup(FireMaterialConfig material, float radius, float height, bool burnsOwnMeshes)
        {
            _material = material;
            _radius = radius;
            _height = height;
            _burnsOwnMeshes = burnsOwnMeshes;
            _ownMeshes = null;
        }

        private MeshRenderer[] GetOwnMeshes()
        {
            if (_ownMeshes == null || Application.isPlaying == false)
                _ownMeshes = GetComponentsInChildren<MeshRenderer>(true);

            return _ownMeshes;
        }

        private bool TryGetMeshBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            foreach (MeshRenderer mesh in GetOwnMeshes())
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(mesh.bounds);
                    continue;
                }

                bounds = mesh.bounds;
                hasBounds = true;
            }

            return hasBounds;
        }

        private void OnDrawGizmosSelected() =>
            DrawSpot();

        private void OnDrawGizmos()
        {
            if (_burnsOwnMeshes == false)
                DrawSpot();
        }

        private void DrawSpot()
        {
            Gizmos.color = _gizmoColor;
            Vector3 bottom = SpotPosition;
            Vector3 top = bottom + Vector3.up * _height;
            Vector3 previous = bottom + Vector3.right * _radius;

            for (int segment = 1; segment <= GIZMO_SEGMENTS; segment++)
            {
                float angle = segment * FULL_TURN / GIZMO_SEGMENTS;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _radius;
                Gizmos.DrawLine(previous, bottom + offset);
                Gizmos.DrawLine(previous - bottom + top, top + offset);
                previous = bottom + offset;
            }

            Gizmos.DrawLine(bottom, top);
        }
    }
}
