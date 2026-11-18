using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Level;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Hydrant.Factory
{
    public class HydrantFactory : IHydrantFactory
    {
        private readonly FactoryConfig _config;
        private readonly WorldTag _worldTag;
        private readonly IObjectResolver _resolver;

        public HydrantFactory(FactoryConfig config, WorldTag worldTag, IObjectResolver resolver)
        {
            _config = config;
            _worldTag = worldTag;
            _resolver = resolver;
        }

        public UniTask<GameObject> CreateAsync() =>
            UniTask.FromResult(_resolver.Instantiate(_config.HydrantPrefab, _worldTag.HydrantPoint));
    }
}
