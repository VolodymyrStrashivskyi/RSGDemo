using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Interaction;
using _Project.Develop.Runtime.Gameplay.WaterStream;
using _Project.Develop.Runtime.Utils;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    [RequireComponent(typeof(CharacterController))]
    public class FirefighterBody : MonoBehaviour, IWaterPushable
    {
        private const float GROUNDED_VERTICAL_VELOCITY = -2f;

        [SerializeField] private CharacterController _characterController;
        [SerializeField] private FirefighterAnimator _animator;
        [SerializeField] private Transform _headPoint;
        [SerializeField] private GrabAnchor _grabAnchor;
        [SerializeField] private FirefighterHands _hands;

        private readonly List<IMovementConstraint> _constraints = new List<IMovementConstraint>();

        private FirefighterConfig _config;
        private Vector3 _moveDirection;
        private bool _isRunning;
        private bool _isJumping;
        private Vector3 _pushVelocity;
        private Vector3 _recoilForce;
        private float _verticalVelocity;
        private float _airTime;

        [Inject]
        public void Construct(FirefighterConfig config) =>
            _config = config;

        public Transform HeadPoint => _headPoint;

        public GrabAnchor GrabAnchor => _grabAnchor;

        public FirefighterHands Hands => _hands;

        public void Move(Vector3 direction, bool isRunning)
        {
            Vector3 planarDirection = direction.WithoutHeight();
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

        public void AddConstraint(IMovementConstraint constraint)
        {
            if (_constraints.Contains(constraint) == false)
                _constraints.Add(constraint);
        }

        public void RemoveConstraint(IMovementConstraint constraint) =>
            _constraints.Remove(constraint);

        public void SetRecoilForce(Vector3 force) =>
            _recoilForce = force.WithoutHeight();

        public void AddWaterPush(Vector3 impulse)
        {
            Vector3 planarImpulse = impulse.WithoutHeight();
            _pushVelocity = Vector3.ClampMagnitude(_pushVelocity + planarImpulse / _config.Mass, _config.MaxPushSpeed);
        }

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

            DampPush();
            ApplyRecoil();

            Vector3 velocity = horizontalVelocity + _pushVelocity + Vector3.up * _verticalVelocity;
            Vector3 displacement = Constrain(velocity * Time.deltaTime);
            _characterController.Move(displacement);

            _airTime = _characterController.isGrounded ? 0f : _airTime + Time.deltaTime;

            bool isAirborne = _isJumping || _airTime > _config.AirborneAnimationDelay;
            _animator.SetSpeed(GetPlanarSpeed(displacement));
            _animator.SetAirborne(isAirborne);
        }

        private void DampPush()
        {
            float drag = _characterController.isGrounded ? _config.PushGroundDrag : _config.PushAirDrag;
            _pushVelocity *= Smoothing.GetDecay(drag, Time.deltaTime);
        }

        private void ApplyRecoil()
        {
            float brace = _characterController.isGrounded ? _config.RecoilBraceForce : 0f;
            float excess = _recoilForce.magnitude - brace;

            if (excess <= 0f)
                return;

            Vector3 acceleration = _recoilForce.normalized * (excess / _config.Mass);
            _pushVelocity = Vector3.ClampMagnitude(_pushVelocity + acceleration * Time.deltaTime, _config.MaxPushSpeed);
        }

        private Vector3 Constrain(Vector3 displacement)
        {
            if (_constraints.Count == 0)
                return displacement;

            Vector3 currentPosition = transform.position;
            Vector3 desiredPosition = currentPosition + displacement.WithoutHeight();

            foreach (IMovementConstraint constraint in _constraints)
                desiredPosition = constraint.Constrain(currentPosition, desiredPosition);

            return new Vector3(desiredPosition.x - currentPosition.x, displacement.y, desiredPosition.z - currentPosition.z);
        }

        private float GetPlanarSpeed(Vector3 displacement)
        {
            if (Time.deltaTime <= 0f)
                return 0f;

            Vector3 planarDisplacement = displacement.WithoutHeight();
            return planarDisplacement.magnitude / Time.deltaTime;
        }
    }
}
