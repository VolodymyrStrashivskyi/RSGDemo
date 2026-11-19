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
        private const string NO_WATER_LABEL = "No water";
        private const string TANK_LABEL = "Tank";
        private const string HYDRANT_LABEL = "Hydrant";
        private const string TANK_VALUE_FORMAT = "{0}%";
        private const string PRESSURE_VALUE_FORMAT = "{0:0.0} bar";
        private const float PERCENT_SCALE = 100f;

        private readonly IWaterSupplyChain _waterSupplyChain;
        private readonly WaterSupplyConfig _config;

        private WaterStatusView _view;

        public WaterStatusPresenter(IWaterSupplyChain waterSupplyChain, WaterSupplyConfig config)
        {
            _waterSupplyChain = waterSupplyChain;
            _config = config;
        }

        public void Bind(WaterStatusView view)
        {
            _view = view;
            OnSourceChanged(_waterSupplyChain.Source);
            _waterSupplyChain.SourceChanged += OnSourceChanged;
        }

        public void Tick()
        {
            if (_view == null)
                return;

            float tankRatio = _waterSupplyChain.TankFillRatio;
            int tankPercent = Mathf.RoundToInt(tankRatio * PERCENT_SCALE);
            float pressure = _waterSupplyChain.Pressure;
            float pressureRatio = _config.PumpPressure > 0f ? pressure / _config.PumpPressure : 0f;

            _view.SetTankLevel(tankRatio, string.Format(CultureInfo.InvariantCulture, TANK_VALUE_FORMAT, tankPercent));
            _view.SetPressure(pressureRatio, string.Format(CultureInfo.InvariantCulture, PRESSURE_VALUE_FORMAT, pressure));
        }

        public void Dispose() =>
            _waterSupplyChain.SourceChanged -= OnSourceChanged;

        private void OnSourceChanged(WaterSourceKind source) =>
            _view.SetSource(GetLabel(source));

        private static string GetLabel(WaterSourceKind source)
        {
            switch (source)
            {
                case WaterSourceKind.Tank:
                    return TANK_LABEL;

                case WaterSourceKind.Hydrant:
                    return HYDRANT_LABEL;

                default:
                    return NO_WATER_LABEL;
            }
        }
    }
}
