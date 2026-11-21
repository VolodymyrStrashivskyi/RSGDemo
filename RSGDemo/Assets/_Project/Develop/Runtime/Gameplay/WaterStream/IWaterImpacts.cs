using System;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public interface IWaterImpacts
    {
        event Action<WaterImpact> Landed;

        void Report(WaterImpact impact);
    }
}
