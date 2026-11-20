using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack.Factory
{
    public class AttackHoseFactory : IAttackHoseFactory
    {
        private readonly FactoryConfig _config;
        private readonly IObjectResolver _resolver;

        public AttackHoseFactory(FactoryConfig config, IObjectResolver resolver)
        {
            _config = config;
            _resolver = resolver;
        }

        public UniTask<AttackHose> CreateAsync(HoseReel reel, Transform nozzleHook)
        {
            AttackHose hose = _resolver.Instantiate(_config.AttackHosePrefab);
            hose.Bind(reel, nozzleHook);
            return UniTask.FromResult(hose);
        }
    }
}
