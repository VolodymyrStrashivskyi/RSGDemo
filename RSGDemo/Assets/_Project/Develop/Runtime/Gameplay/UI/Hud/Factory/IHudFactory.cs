using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Factory
{
    public interface IHudFactory
    {
        UniTask<HudView> CreateAsync();
    }
}
