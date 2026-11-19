using System;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public class SupplyLine : IWaterSource, IDisposable
    {
        public event Action<string> Connected;
        public event Action<string> Disconnected;
        public event Action FlowChanged;

        private readonly HydrantSource _hydrant;

        public SupplyLine(HydrantSource hydrant)
        {
            _hydrant = hydrant;
            _hydrant.Opened += OnHydrantChanged;
            _hydrant.Closed += OnHydrantChanged;
        }

        public bool IsConnected { get; private set; }

        public bool IsAvailable => IsConnected && _hydrant.IsAvailable;

        public float Draw(float liters) =>
            IsConnected ? _hydrant.Draw(liters) : 0f;

        public void Connect(string initiator)
        {
            if (IsConnected)
                return;

            IsConnected = true;
            Connected?.Invoke(initiator);
            FlowChanged?.Invoke();
        }

        public void Disconnect(string initiator)
        {
            if (IsConnected == false)
                return;

            IsConnected = false;
            Disconnected?.Invoke(initiator);
            FlowChanged?.Invoke();
        }

        public void Dispose()
        {
            _hydrant.Opened -= OnHydrantChanged;
            _hydrant.Closed -= OnHydrantChanged;
        }

        private void OnHydrantChanged(string initiator)
        {
            if (IsConnected)
                FlowChanged?.Invoke();
        }
    }
}
