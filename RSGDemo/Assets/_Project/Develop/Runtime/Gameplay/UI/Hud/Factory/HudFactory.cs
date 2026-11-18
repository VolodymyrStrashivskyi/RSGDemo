using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Factory
{
    public class HudFactory : IHudFactory
    {
        private readonly FactoryConfig _config;
        private readonly IObjectResolver _resolver;

        public HudFactory(FactoryConfig config, IObjectResolver resolver)
        {
            _config = config;
            _resolver = resolver;
        }

        public UniTask<HudView> CreateAsync() =>
            UniTask.FromResult(_resolver.Instantiate(_config.HudPrefab));
    }
}
