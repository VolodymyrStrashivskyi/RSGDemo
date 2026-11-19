namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public interface IWaterSource
    {
        bool IsAvailable { get; }

        float Draw(float liters);
    }
}
