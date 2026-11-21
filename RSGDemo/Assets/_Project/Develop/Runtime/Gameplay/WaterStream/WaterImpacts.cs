using System;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class WaterImpacts : IWaterImpacts
    {
        public event Action<WaterImpact> Landed;

        public void Report(WaterImpact impact) =>
            Landed?.Invoke(impact);
    }
}
