using System;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public class WaterSupplyChain : IWaterSupplyChain, ITickable, IDisposable
    {
        public event Action<WaterSourceKind> SourceChanged;
        public event Action<SupplyFailure> FailureOccurred;

        private readonly HydrantSource _hydrant;
        private readonly SupplyLine _supplyLine;
        private readonly WaterTank _tank;
        private readonly Pump _pump;

        private float _demand;

        public WaterSupplyChain(HydrantSource hydrant, SupplyLine supplyLine, WaterTank tank, Pump pump)
        {
            _hydrant = hydrant;
            _supplyLine = supplyLine;
            _tank = tank;
            _pump = pump;

            _hydrant.Closed += OnHydrantClosed;
            _supplyLine.Disconnected += OnSupplyLineDisconnected;
            _tank.Emptied += OnTankEmptied;
            _pump.SourceChanged += OnSourceChanged;
        }

        public WaterSourceKind Source => _pump.Source;

        public float Pressure => _pump.Pressure;

        public float TankFillRatio => _tank.FillRatio;

        public float DeliveredFlow { get; private set; }

        public void SetDemand(float litersPerSecond) =>
            _demand = Mathf.Max(0f, litersPerSecond);

        public void Tick()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            float delivered = _pump.Supply(_demand * deltaTime, deltaTime);
            DeliveredFlow = delivered / deltaTime;
        }

        public void Dispose()
        {
            _hydrant.Closed -= OnHydrantClosed;
            _supplyLine.Disconnected -= OnSupplyLineDisconnected;
            _tank.Emptied -= OnTankEmptied;
            _pump.SourceChanged -= OnSourceChanged;
        }

        private void OnHydrantClosed(string initiator)
        {
            if (_supplyLine.IsConnected)
                Report(SupplyFailureKind.HydrantClosed, initiator);
        }

        private void OnSupplyLineDisconnected(string initiator)
        {
            if (_hydrant.IsOpen)
                Report(SupplyFailureKind.SupplyLineDisconnected, initiator);
        }

        private void OnTankEmptied() =>
            Report(SupplyFailureKind.TankEmptied, null);

        private void OnSourceChanged(WaterSourceKind source) =>
            SourceChanged?.Invoke(source);

        private void Report(SupplyFailureKind kind, string initiator) =>
            FailureOccurred?.Invoke(new SupplyFailure(kind, initiator, Time.time));
    }
}
