using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    [RequireComponent(typeof(CharacterController))]
    public class FirefighterBody : MonoBehaviour
    {
        private const float GROUNDED_VERTICAL_VELOCITY = -2f;

        [SerializeField] private CharacterController _characterController;
        [SerializeField] private FirefighterAnimator _animator;
        [SerializeField] private Transform _headPoint;

        private FirefighterConfig _config;
        private Vector3 _moveDirection;
        private bool _isRunning;
        private bool _isJumping;
        private float _verticalVelocity;
        private float _airTime;

        [Inject]
        public void Construct(FirefighterConfig config) =>
            _config = config;

        public Transform HeadPoint => _headPoint;

        public void Move(Vector3 direction, bool isRunning)
        {
            Vector3 planarDirection = new Vector3(direction.x, 0f, direction.z);
            _moveDirection = Vector3.ClampMagnitude(planarDirection, 1f);
            _isRunning = isRunning;
        }

        public void Jump()
        {
            if (_characterController.isGrounded == false)
                return;

            _verticalVelocity = Mathf.Sqrt(2f * _config.JumpHeight * -_config.Gravity);
            _isJumping = true;
        }

        public void SetYaw(float yaw) =>
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        private void Update()
        {
            float speed = _isRunning ? _config.RunSpeed : _config.WalkSpeed;
            Vector3 horizontalVelocity = _moveDirection * speed;

            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = GROUNDED_VERTICAL_VELOCITY;
                _isJumping = false;
            }

            _verticalVelocity += _config.Gravity * Time.deltaTime;

            Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;
            _characterController.Move(velocity * Time.deltaTime);

            _airTime = _characterController.isGrounded ? 0f : _airTime + Time.deltaTime;

            bool isAirborne = _isJumping || _airTime > _config.AirborneAnimationDelay;
            _animator.SetSpeed(horizontalVelocity.magnitude);
            _animator.SetAirborne(isAirborne);
        }
    }
}
