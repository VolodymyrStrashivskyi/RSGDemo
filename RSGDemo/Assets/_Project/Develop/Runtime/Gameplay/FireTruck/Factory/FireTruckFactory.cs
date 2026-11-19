using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Level;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.Factory
{
    public class FireTruckFactory : IFireTruckFactory
    {
        private readonly FactoryConfig _config;
        private readonly WorldTag _worldTag;
        private readonly IObjectResolver _resolver;

        public FireTruckFactory(FactoryConfig config, WorldTag worldTag, IObjectResolver resolver)
        {
            _config = config;
            _worldTag = worldTag;
            _resolver = resolver;
        }

        public UniTask<FireTruckView> CreateAsync() =>
            UniTask.FromResult(_resolver.Instantiate(_config.FireTruckPrefab, _worldTag.FireTruckPoint));
    }
}
