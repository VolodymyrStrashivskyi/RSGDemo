using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.Hydrant.Factory
{
    public interface IHydrantFactory
    {
        UniTask<HydrantView> CreateAsync();
    }
}
