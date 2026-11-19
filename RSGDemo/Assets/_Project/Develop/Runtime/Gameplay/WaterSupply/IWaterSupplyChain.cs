using System;

namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public interface IWaterSupplyChain
    {
        event Action<WaterSourceKind> SourceChanged;
        event Action<SupplyFailure> FailureOccurred;

        WaterSourceKind Source { get; }
        float Pressure { get; }
        float TankFillRatio { get; }
        float DeliveredFlow { get; }

        void SetDemand(float litersPerSecond);
    }
}
