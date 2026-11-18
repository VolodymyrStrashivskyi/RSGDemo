using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory
{
    public class FirstPersonCameraFactory : IFirstPersonCameraFactory
    {
        private readonly FactoryConfig _config;
        private readonly IObjectResolver _resolver;

        public FirstPersonCameraFactory(FactoryConfig config, IObjectResolver resolver)
        {
            _config = config;
            _resolver = resolver;
        }

        public UniTask<FirstPersonCamera> CreateAsync(Transform headPoint)
        {
            FirstPersonCamera camera = _resolver.Instantiate(_config.CameraPrefab, headPoint.position, headPoint.rotation);
            return UniTask.FromResult(camera);
        }
    }
}
