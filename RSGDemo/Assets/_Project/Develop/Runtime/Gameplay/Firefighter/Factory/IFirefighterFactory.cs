using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Factory
{
    public interface IFirefighterFactory
    {
        UniTask<FirefighterBody> CreateAsync();
    }
}
