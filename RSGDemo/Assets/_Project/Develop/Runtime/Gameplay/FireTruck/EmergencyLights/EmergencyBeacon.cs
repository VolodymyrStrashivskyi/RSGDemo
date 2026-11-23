using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.EmergencyLights
{
    public class EmergencyBeacon : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _emissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Renderer _lens;
        [SerializeField] private Light _light;

        private MaterialPropertyBlock _propertyBlock;
        private EmergencyLightsConfig _config;
        private Color _color;

        public void Setup(EmergencyLightsConfig config, Color color)
        {
            _config = config;
            _color = color;
            _propertyBlock = new MaterialPropertyBlock();

            _light.color = color;
            _light.range = config.LightRange;
            _light.shadows = LightShadows.None;

            SetBrightness(0f);
        }

        public void SetBrightness(float brightness)
        {
            Color lensColor = _color * Mathf.Lerp(_config.LensOffBrightness, 1f, brightness);
            lensColor.a = 1f;

            _propertyBlock.SetColor(_baseColorId, lensColor);
            _propertyBlock.SetColor(_emissionColorId, _color * (_config.LensEmission * brightness));
            _lens.SetPropertyBlock(_propertyBlock);

            _light.intensity = _config.LightIntensity * brightness;
            _light.enabled = brightness > 0f;
        }
    }
}
