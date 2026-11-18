using _Project.Develop.Runtime.Data.Configs;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair
{
    public class CrosshairView : MonoBehaviour
    {
        private static readonly int _outerRadiusId = Shader.PropertyToID("_OuterRadius");
        private static readonly int _innerRadiusId = Shader.PropertyToID("_InnerRadius");

        [SerializeField] private Image _image;

        private CrosshairConfig _config;
        private Material _material;
        private float _highlight;
        private Tween _tween;

        [Inject]
        public void Construct(CrosshairConfig config)
        {
            _config = config;
            _material = new Material(_image.material);
            _image.material = _material;
            ApplyHighlight(0f);
        }

        public void SetHighlighted(bool isHighlighted)
        {
            float target = isHighlighted ? 1f : 0f;
            TweenSettings settings = isHighlighted ? _config.HighlightTween : _config.UnhighlightTween;

            _tween.Stop();
            _tween = Tween.Custom(this, new TweenSettings<float>(_highlight, target, settings), (view, value) => view.ApplyHighlight(value));
        }

        private void OnDestroy()
        {
            _tween.Stop();

            if (_material != null)
                Destroy(_material);
        }

        private void ApplyHighlight(float highlight)
        {
            _highlight = highlight;
            _material.SetFloat(_outerRadiusId, Mathf.LerpUnclamped(_config.DotRadius, _config.RingOuterRadius, highlight));
            _material.SetFloat(_innerRadiusId, Mathf.Max(0f, Mathf.LerpUnclamped(0f, _config.RingInnerRadius, highlight)));
        }
    }
}
