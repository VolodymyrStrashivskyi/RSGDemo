using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Supply.Factory
{
    public class SupplyHoseFactory : ISupplyHoseFactory
    {
        private readonly FactoryConfig _config;
        private readonly IObjectResolver _resolver;

        public SupplyHoseFactory(FactoryConfig config, IObjectResolver resolver)
        {
            _config = config;
            _resolver = resolver;
        }

        public UniTask<SupplyHose> CreateAsync(Transform intakePoint, Transform layPoint)
        {
            SupplyHose hose = _resolver.Instantiate(_config.SupplyHosePrefab);
            hose.Bind(intakePoint, layPoint);
            return UniTask.FromResult(hose);
        }
    }
}
