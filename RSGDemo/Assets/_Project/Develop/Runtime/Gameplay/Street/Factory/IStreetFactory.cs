using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.Street.Factory
{
    public interface IStreetFactory
    {
        UniTask<StreetView> CreateAsync();
    }
}
