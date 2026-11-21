using System;
using System.Globalization;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus
{
    public class WaterStatusPresenter : ITickable, IDisposable
    {
        private const string TANK_VALUE_FORMAT = "{0}<size=60%><alpha=#99>{1}";
        private const string PRESSURE_VALUE_FORMAT = "{0:0.0}<size=60%><alpha=#99>{1}";
        private const float PERCENT_SCALE = 100f;

        private readonly IWaterSupplyChain _waterSupplyChain;
        private readonly WaterSupplyConfig _supplyConfig;
        private readonly WaterStatusConfig _statusConfig;

        private WaterStatusView _view;
        private float _lastPressure;

        public WaterStatusPresenter(IWaterSupplyChain waterSupplyChain, WaterSupplyConfig supplyConfig, WaterStatusConfig statusConfig)
        {
            _waterSupplyChain = waterSupplyChain;
            _supplyConfig = supplyConfig;
            _statusConfig = statusConfig;
        }

        public void Bind(WaterStatusView view)
        {
            _view = view;
            _lastPressure = _waterSupplyChain.Pressure;
            _view.SetSource(GetLabel(_waterSupplyChain.Source), false);
            _waterSupplyChain.SourceChanged += OnSourceChanged;
        }

        public void Tick()
        {
            if (_view == null)
                return;

            float tankRatio = _waterSupplyChain.TankFillRatio;
            int tankPercent = Mathf.RoundToInt(tankRatio * PERCENT_SCALE);
            float pressure = _waterSupplyChain.Pressure;
            float pressureRatio = _supplyConfig.PumpPressure > 0f ? pressure / _supplyConfig.PumpPressure : 0f;

            _view.SetTankLow(tankRatio < _statusConfig.LowTankRatio);
            _view.SetTankLevel(tankRatio, string.Format(CultureInfo.InvariantCulture, TANK_VALUE_FORMAT, tankPercent, _statusConfig.TankUnit));
            _view.SetPressure(pressureRatio, string.Format(CultureInfo.InvariantCulture, PRESSURE_VALUE_FORMAT, pressure, _statusConfig.PressureUnit));

            if (_lastPressure - pressure >= _statusConfig.PressureDropToShake)
                _view.Shake();

            _lastPressure = pressure;
        }

        public void Dispose() =>
            _waterSupplyChain.SourceChanged -= OnSourceChanged;

        private void OnSourceChanged(WaterSourceKind source) =>
            _view.SetSource(GetLabel(source), true);

        private string GetLabel(WaterSourceKind source)
        {
            switch (source)
            {
                case WaterSourceKind.Tank:
                    return _statusConfig.TankSourceLabel;

                case WaterSourceKind.Hydrant:
                    return _statusConfig.HydrantSourceLabel;

                default:
                    return _statusConfig.NoWaterLabel;
            }
        }
    }
}
