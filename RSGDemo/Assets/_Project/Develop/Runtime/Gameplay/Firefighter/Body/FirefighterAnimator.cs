using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    public class FirefighterAnimator : MonoBehaviour
    {
        private static readonly int _speedHash = Animator.StringToHash("Speed");
        private static readonly int _isAirborneHash = Animator.StringToHash("IsAirborne");

        [SerializeField] private Animator _animator;

        private FirefighterConfig _config;

        [Inject]
        public void Construct(FirefighterConfig config) =>
            _config = config;

        public void SetSpeed(float speed) =>
            _animator.SetFloat(_speedHash, speed, _config.AnimationDampTime, Time.deltaTime);

        public void SetAirborne(bool isAirborne) =>
            _animator.SetBool(_isAirborneHash, isAirborne);
    }
}
