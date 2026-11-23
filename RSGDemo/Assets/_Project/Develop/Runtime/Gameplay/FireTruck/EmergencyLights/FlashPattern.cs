using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.EmergencyLights
{
    public class FlashPattern
    {
        private readonly EmergencyLightsConfig _config;
        private readonly int _beaconCount;

        public FlashPattern(EmergencyLightsConfig config, int beaconCount)
        {
            _config = config;
            _beaconCount = beaconCount;
        }

        public bool IsLit(int beaconIndex, float time)
        {
            float cyclePosition = Mathf.Repeat(time, _config.CycleDuration) / _config.CycleDuration;
            float turnPosition = cyclePosition * _beaconCount;
            int activeBeacon = Mathf.FloorToInt(turnPosition);

            if (activeBeacon != beaconIndex)
                return false;

            float flashPosition = (turnPosition - activeBeacon) * _config.FlashesPerTurn;
            return flashPosition - Mathf.Floor(flashPosition) < _config.FlashDuty;
        }
    }
}
