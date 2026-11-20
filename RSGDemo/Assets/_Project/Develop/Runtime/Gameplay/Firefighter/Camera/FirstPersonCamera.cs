using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Camera
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class FirstPersonCamera : MonoBehaviour
    {
        private const float NEWTONS_PER_KILONEWTON = 1000f;
        private const float HALF = 0.5f;
        private const float NOISE_RANGE = 2f;
        private const float PITCH_SHAKE_SEED = 0.37f;
        private const float YAW_SHAKE_SEED = 5.13f;

        [SerializeField] private UnityEngine.Camera _camera;
        [SerializeField] private FirefighterHands _arms;

        private CameraConfig _config;
        private float _yaw;
        private float _pitch;
        private FirstPersonViewModel _heldViewModel;
        private float _recoilKilonewtons;
        private float _recoilClimb;
        private float _recoilClimbVelocity;
        private float _pitchNoise;
        private float _yawNoise;

        [Inject]
        public void Construct(CameraConfig config)
        {
            _config = config;
            _camera.fieldOfView = config.FieldOfView;
            _yaw = transform.eulerAngles.y;
            ApplyRotation();
        }

        public FirefighterHands Arms => _arms;

        public float Yaw => _yaw;

        public Quaternion PlanarRotation => Quaternion.Euler(0f, _yaw, 0f);

        public Ray ViewRay => new Ray(transform.position, transform.forward);

        public void Look(Vector2 delta)
        {
            _yaw += delta.x * _config.Sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * _config.Sensitivity, _config.MinPitch, _config.MaxPitch);
            ApplyRotation();
        }

        public void SetRecoilForce(float force) =>
            _recoilKilonewtons = force / NEWTONS_PER_KILONEWTON;

        public void Hold(FirstPersonViewModel viewModel)
        {
            _heldViewModel = viewModel;
            viewModel.Show(transform);
            _arms.Hold(viewModel);
        }

        public void Release(FirstPersonViewModel viewModel)
        {
            _arms.Release();
            viewModel.Hide();

            if (_heldViewModel == viewModel)
                _heldViewModel = null;
        }

        public void SetPosition(Vector3 position) =>
            transform.position = position;

        private void Update()
        {
            if (_config == null)
                return;

            UpdateRecoilClimb(Time.deltaTime);
            UpdateShakeNoise();
            ApplyRotation();
            KickHeldItem();
        }

        private void UpdateRecoilClimb(float deltaTime)
        {
            float targetClimb = _recoilKilonewtons * _config.RecoilClimbPerKilonewton;
            float acceleration = (targetClimb - _recoilClimb) * _config.RecoilStiffness - _recoilClimbVelocity * _config.RecoilDamping;
            _recoilClimbVelocity += acceleration * deltaTime;
            _recoilClimb += _recoilClimbVelocity * deltaTime;
        }

        private void UpdateShakeNoise()
        {
            float time = Time.time * _config.RecoilShakeFrequency;
            _pitchNoise = GetNoise(time, PITCH_SHAKE_SEED);
            _yawNoise = GetNoise(time, YAW_SHAKE_SEED);
        }

        private void KickHeldItem()
        {
            if (_heldViewModel == null)
                return;

            float sideways = _yawNoise * _recoilKilonewtons * _config.HeldShakePerKilonewton;
            Vector3 offset = new Vector3(sideways, 0f, -_recoilKilonewtons * _config.HeldKickPerKilonewton);
            _heldViewModel.SetRecoil(offset, _recoilClimb * _config.HeldTiltRatio);
        }

        private void ApplyRotation()
        {
            float shake = _recoilKilonewtons * _config.RecoilShakePerKilonewton;
            float pitch = Mathf.Clamp(_pitch - _recoilClimb + _pitchNoise * shake, _config.MinPitch, _config.MaxPitch);
            transform.rotation = Quaternion.Euler(pitch, _yaw + _yawNoise * shake, 0f);
        }

        private static float GetNoise(float time, float seed) =>
            (Mathf.PerlinNoise(time, seed) - HALF) * NOISE_RANGE;
    }
}
