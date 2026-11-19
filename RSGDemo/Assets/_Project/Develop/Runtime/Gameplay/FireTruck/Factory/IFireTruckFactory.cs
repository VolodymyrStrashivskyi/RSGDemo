using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.Factory
{
    public interface IFireTruckFactory
    {
        UniTask<FireTruckView> CreateAsync();
    }
}
