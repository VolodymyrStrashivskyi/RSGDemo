using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Level;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Street.Factory
{
    public class StreetFactory : IStreetFactory
    {
        private readonly FactoryConfig _config;
        private readonly WorldTag _worldTag;
        private readonly IObjectResolver _resolver;

        public StreetFactory(FactoryConfig config, WorldTag worldTag, IObjectResolver resolver)
        {
            _config = config;
            _worldTag = worldTag;
            _resolver = resolver;
        }

        public UniTask<StreetView> CreateAsync() =>
            UniTask.FromResult(_resolver.Instantiate(_config.StreetPrefab, _worldTag.StreetPoint));
    }
}
