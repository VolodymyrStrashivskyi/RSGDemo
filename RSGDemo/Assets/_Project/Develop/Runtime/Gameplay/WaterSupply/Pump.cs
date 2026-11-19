using System;
using _Project.Develop.Runtime.Data.Configs;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public class Pump
    {
        public event Action<WaterSourceKind> SourceChanged;

        private readonly SupplyLine _supplyLine;
        private readonly WaterTank _tank;
        private readonly WaterSupplyConfig _config;

        public Pump(SupplyLine supplyLine, WaterTank tank, WaterSupplyConfig config)
        {
            _supplyLine = supplyLine;
            _tank = tank;
            _config = config;
            Source = SelectSource();
        }

        public WaterSourceKind Source { get; private set; }

        public float Pressure => Source == WaterSourceKind.None ? 0f : _config.PumpPressure;

        public float Supply(float liters, float deltaTime)
        {
            UpdateSource();

            switch (Source)
            {
                case WaterSourceKind.Hydrant:
                    _tank.Refill(_config.TankRefillRate * deltaTime);
                    return _supplyLine.Draw(liters);

                case WaterSourceKind.Tank:
                    return _tank.Draw(liters);

                default:
                    return 0f;
            }
        }

        private void UpdateSource()
        {
            WaterSourceKind source = SelectSource();

            if (source == Source)
                return;

            Source = source;
            SourceChanged?.Invoke(Source);
        }

        private WaterSourceKind SelectSource()
        {
            if (_supplyLine.IsAvailable)
                return WaterSourceKind.Hydrant;

            return _tank.IsAvailable ? WaterSourceKind.Tank : WaterSourceKind.None;
        }
    }
}
