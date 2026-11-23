using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.EmergencyLights
{
    public class EmergencyLightBar : MonoBehaviour
    {
        private const float DARK_THRESHOLD = 0.01f;

        [SerializeField] private EmergencyLightsConfig _config;
        [SerializeField] private EmergencyBeacon[] _beacons;

        private FlashPattern _pattern;
        private float[] _brightness;

        private void Awake()
        {
            _pattern = new FlashPattern(_config, _beacons.Length);
            _brightness = new float[_beacons.Length];

            for (int i = 0; i < _beacons.Length; i++)
                _beacons[i].Setup(_config, GetBeaconColor(i));
        }

        private void Update()
        {
            float blend = Smoothing.GetBlend(_config.FadeSharpness, Time.deltaTime);

            for (int i = 0; i < _beacons.Length; i++)
            {
                float target = _pattern.IsLit(i, Time.time) ? 1f : 0f;
                float brightness = Mathf.Lerp(_brightness[i], target, blend);

                if (brightness < DARK_THRESHOLD)
                    brightness = 0f;

                _brightness[i] = brightness;
                _beacons[i].SetBrightness(brightness);
            }
        }

        private Color GetBeaconColor(int beaconIndex) =>
            _config.BeaconColors[beaconIndex % _config.BeaconColors.Length];
    }
}
