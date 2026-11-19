using _Project.Develop.Runtime.Gameplay.Interaction;
using PrimeTween;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    public class FirefighterHands : MonoBehaviour
    {
        [SerializeField] private Transform _leftUpperArm;
        [SerializeField] private Transform _leftLowerArm;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightUpperArm;
        [SerializeField] private Transform _rightLowerArm;
        [SerializeField] private Transform _rightHand;
        [SerializeField, Tooltip("Direction from the shoulder the left elbow points to, in body space")]
        private Vector3 _leftElbowHint = new Vector3(-0.6f, -0.5f, -0.2f);
        [SerializeField, Tooltip("Direction from the shoulder the right elbow points to, in body space")]
        private Vector3 _rightElbowHint = new Vector3(0.6f, -0.5f, -0.2f);
        [SerializeField, Tooltip("Local axis of the left hand bone that points out of the back of the hand")]
        private Vector3 _leftBackOfHandAxis = Vector3.left;
        [SerializeField, Tooltip("Local axis of the right hand bone that points out of the back of the hand")]
        private Vector3 _rightBackOfHandAxis = Vector3.right;
        [SerializeField] private TweenSettings _gripTween = new TweenSettings(0.25f, Ease.OutCubic);
        [SerializeField] private TweenSettings _releaseTween = new TweenSettings(0.2f, Ease.InCubic);

        private TwoBoneIk _leftArm;
        private TwoBoneIk _rightArm;
        private IHandGrips _grips;
        private float _weight;
        private Tween _tween;

        public void Hold(IHandGrips grips)
        {
            _grips = grips;
            AnimateWeight(1f, _gripTween);
        }

        public void Release() =>
            AnimateWeight(0f, _releaseTween);

        private void Awake()
        {
            _leftArm = new TwoBoneIk(_leftUpperArm, _leftLowerArm, _leftHand, _leftBackOfHandAxis);
            _rightArm = new TwoBoneIk(_rightUpperArm, _rightLowerArm, _rightHand, _rightBackOfHandAxis);
        }

        private void LateUpdate()
        {
            if (_grips == null || _weight <= 0f)
                return;

            Vector3 leftGrip = _grips.LeftHandGrip.position;
            Vector3 rightGrip = _grips.RightHandGrip.position;

            _leftArm.Solve(leftGrip, GetElbowHint(_leftUpperArm, _leftElbowHint), _weight);
            _rightArm.Solve(rightGrip, GetElbowHint(_rightUpperArm, _rightElbowHint), _weight);
            _leftArm.OrientEnd(leftGrip - rightGrip, _weight);
            _rightArm.OrientEnd(rightGrip - leftGrip, _weight);
        }

        private void OnDestroy() =>
            _tween.Stop();

        private Vector3 GetElbowHint(Transform upperArm, Vector3 localDirection) =>
            upperArm.position + transform.TransformDirection(localDirection);

        private void AnimateWeight(float target, TweenSettings settings)
        {
            _tween.Stop();
            _tween = Tween.Custom(this, new TweenSettings<float>(_weight, target, settings), (hands, value) => hands._weight = value);
        }
    }
}
