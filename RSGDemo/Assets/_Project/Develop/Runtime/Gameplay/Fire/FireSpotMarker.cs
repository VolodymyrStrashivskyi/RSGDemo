using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSpotMarker : MonoBehaviour
    {
        private const int GIZMO_SEGMENTS = 16;
        private const float FULL_TURN = Mathf.PI * 2f;

        private static readonly Color GizmoColor = new Color(1f, 0.45f, 0.1f);

        [SerializeField, Tooltip("What burns here")]
        private FireMaterialConfig _material;
        [SerializeField, Min(0.05f), Tooltip("Radius of the burning area around the marker, m")]
        private float _radius = 0.5f;
        [SerializeField, Min(0.1f), Tooltip("Height of the flames above the marker, m")]
        private float _height = 1.2f;

        public string Id => name;

        public FireMaterialConfig Material => _material;

        public float Radius => _radius;

        public float Height => _height;

        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            Vector3 bottom = transform.position;
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
