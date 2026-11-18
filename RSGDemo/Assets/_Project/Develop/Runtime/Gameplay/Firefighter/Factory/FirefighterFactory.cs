using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Level;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Factory
{
    public class FirefighterFactory : IFirefighterFactory
    {
        private readonly FactoryConfig _config;
        private readonly WorldTag _worldTag;
        private readonly IObjectResolver _resolver;

        public FirefighterFactory(FactoryConfig config, WorldTag worldTag, IObjectResolver resolver)
        {
            _config = config;
            _worldTag = worldTag;
            _resolver = resolver;
        }

        public UniTask<FirefighterBody> CreateAsync()
        {
            Transform point = _worldTag.PlayerPoint;
            FirefighterBody body = _resolver.Instantiate(_config.FirefighterPrefab, point.position, point.rotation);
            return UniTask.FromResult(body);
        }
    }
}
