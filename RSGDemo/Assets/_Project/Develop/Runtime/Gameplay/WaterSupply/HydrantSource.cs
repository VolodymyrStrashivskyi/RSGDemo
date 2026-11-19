using System;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public class HydrantSource : IWaterSource
    {
        public event Action<string> Opened;
        public event Action<string> Closed;

        public bool IsOpen { get; private set; }

        public bool IsAvailable => IsOpen;

        public float Draw(float liters) =>
            IsOpen ? liters : 0f;

        public void Open(string initiator)
        {
            if (IsOpen)
                return;

            IsOpen = true;
            Opened?.Invoke(initiator);
        }

        public void Close(string initiator)
        {
            if (IsOpen == false)
                return;

            IsOpen = false;
            Closed?.Invoke(initiator);
        }
    }
}
