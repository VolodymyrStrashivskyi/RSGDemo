using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Configs/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f)] public float Sensitivity { get; private set; } = 0.1f;
        [field: SerializeField, Range(-89f, 0f)] public float MinPitch { get; private set; } = -80f;
        [field: SerializeField, Range(0f, 89f)] public float MaxPitch { get; private set; } = 80f;
        [field: SerializeField, Range(30f, 110f)] public float FieldOfView { get; private set; } = 70f;
    }
}
