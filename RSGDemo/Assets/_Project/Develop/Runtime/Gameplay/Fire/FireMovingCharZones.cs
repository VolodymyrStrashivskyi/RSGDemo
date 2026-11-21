using System;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireMovingCharZones : ITickable, IDisposable
    {
        private const int MAX_ZONES = 16;
        private const float MIN_VISIBLE = 0.001f;

        private static readonly int _zonesId = Shader.PropertyToID("_MovingCharZones");
        private static readonly int _zoneStatesId = Shader.PropertyToID("_MovingCharZoneStates");
        private static readonly int _zoneCountId = Shader.PropertyToID("_MovingCharZoneCount");

        private readonly IFireSystem _fireSystem;
        private readonly FireVisualConfig _config;
        private readonly Vector4[] _zones = new Vector4[MAX_ZONES];
        private readonly Vector4[] _zoneStates = new Vector4[MAX_ZONES];

        public FireMovingCharZones(IFireSystem fireSystem, FireVisualConfig config)
        {
            _fireSystem = fireSystem;
            _config = config;
        }

        public void Tick()
        {
            int count = 0;

            foreach (FireSpot spot in _fireSystem.Spots)
            {
                if (spot.IsMovable == false)
                    continue;

                float charAmount = FireCharLook.GetShownChar(spot, _config);
                float heat = spot.GetGlow(_config.EmberLightShare);

                if (charAmount < MIN_VISIBLE && heat < MIN_VISIBLE)
                    continue;

                if (count == MAX_ZONES)
                    break;

                _zones[count] = new Vector4(spot.Position.x, spot.Position.y, spot.Position.z, spot.Radius);
                _zoneStates[count] = new Vector4(spot.Height, charAmount, heat, FireCharLook.GetReach(spot, _config));
                count++;
            }

            Shader.SetGlobalVectorArray(_zonesId, _zones);
            Shader.SetGlobalVectorArray(_zoneStatesId, _zoneStates);
            Shader.SetGlobalFloat(_zoneCountId, count);
        }

        public void Dispose() =>
            Shader.SetGlobalFloat(_zoneCountId, 0f);
    }
}
