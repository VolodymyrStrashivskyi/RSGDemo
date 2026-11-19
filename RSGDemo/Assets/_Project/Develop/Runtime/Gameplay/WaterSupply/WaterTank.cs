using System;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public class WaterTank : IWaterSource
    {
        public event Action Emptied;

        private readonly WaterSupplyConfig _config;

        public WaterTank(WaterSupplyConfig config)
        {
            _config = config;
            Amount = config.TankCapacity;
        }

        public float Amount { get; private set; }

        public float Capacity => _config.TankCapacity;

        public float FillRatio => Capacity > 0f ? Amount / Capacity : 0f;

        public bool IsEmpty => Amount <= 0f;

        public bool IsAvailable => IsEmpty == false;

        public float Draw(float liters)
        {
            if (IsEmpty)
                return 0f;

            float drawn = Mathf.Min(liters, Amount);
            Amount -= drawn;

            if (IsEmpty)
                Emptied?.Invoke();

            return drawn;
        }

        public void Refill(float liters) =>
            Amount = Mathf.Min(Capacity, Amount + liters);
    }
}
