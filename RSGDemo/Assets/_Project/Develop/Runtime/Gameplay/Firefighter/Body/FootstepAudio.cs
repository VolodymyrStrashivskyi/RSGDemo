using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.House;
using _Project.Develop.Runtime.Utils;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    public class FootstepAudio : MonoBehaviour
    {
        private const float PROBE_LIFT = 0.1f;

        [SerializeField] private CharacterController _characterController;
        [SerializeField] private FootstepConfig _config;

        private IAudioService _audioService;
        private IRaycastService _raycastService;
        private float _walked;
        private float _lastStepTime;

        [Inject]
        public void Construct(IAudioService audioService, IRaycastService raycastService)
        {
            _audioService = audioService;
            _raycastService = raycastService;
        }

        private void Update()
        {
            if (_audioService == null || _characterController.isGrounded == false)
                return;

            float speed = _characterController.velocity.WithoutHeight().magnitude;

            if (speed < _config.MinSpeed)
            {
                _walked = 0f;
                return;
            }

            _walked += speed * Time.deltaTime;

            if (_walked < _config.StepLength || Time.time - _lastStepTime < _config.MinInterval)
                return;

            _walked = 0f;
            _lastStepTime = Time.time;
            Step();
        }

        private void Step()
        {
            Vector3 feet = transform.position;
            Ray ray = new Ray(feet + Vector3.up * PROBE_LIFT, Vector3.down);
            bool isIndoor = _raycastService.TryRaycast(ray, _config.GroundProbe, _config.GroundMask, out RaycastHit hit)
                && hit.collider.GetComponentInParent<HouseView>() != null;

            _audioService.Play(isIndoor ? _config.Indoor : _config.Outdoor, feet);
        }
    }
}
