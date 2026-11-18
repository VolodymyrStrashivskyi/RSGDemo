using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class FirstPersonCamera : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _camera;

        private CameraConfig _config;
        private float _yaw;
        private float _pitch;

        [Inject]
        public void Construct(CameraConfig config)
        {
            _config = config;
            _camera.fieldOfView = config.FieldOfView;
            _yaw = transform.eulerAngles.y;
            ApplyRotation();
        }

        public float Yaw => _yaw;

        public Quaternion PlanarRotation => Quaternion.Euler(0f, _yaw, 0f);

        public void Look(Vector2 delta)
        {
            _yaw += delta.x * _config.Sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * _config.Sensitivity, _config.MinPitch, _config.MaxPitch);
            ApplyRotation();
        }

        public void SetPosition(Vector3 position) =>
            transform.position = position;

        private void ApplyRotation() =>
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}
