using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Fire.Factory
{
    public class FireFactory : IFireFactory
    {
        private readonly FactoryConfig _config;
        private readonly IFireSystem _fireSystem;
        private readonly IObjectResolver _resolver;

        public FireFactory(FactoryConfig config, IFireSystem fireSystem, IObjectResolver resolver)
        {
            _config = config;
            _fireSystem = fireSystem;
            _resolver = resolver;
        }

        public UniTask CreateAsync(IReadOnlyList<FireSpotMarker> markers)
        {
            foreach (FireSpotMarker marker in markers)
            {
                FireSpot spot = new FireSpot(marker.Id, marker.transform.position, marker.Radius, marker.Height, marker.Material);
                _fireSystem.Register(spot);

                FireSpotView view = _resolver.Instantiate(_config.FireSpotPrefab, marker.transform);
                view.Bind(spot);
            }

            return UniTask.CompletedTask;
        }
    }
}
