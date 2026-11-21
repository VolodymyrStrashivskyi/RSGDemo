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
        private readonly IFireLightBudget _lightBudget;
        private readonly FireVisualConfig _visualConfig;

        public FireFactory(FactoryConfig config, IFireSystem fireSystem, IObjectResolver resolver,
            IFireLightBudget lightBudget, FireVisualConfig visualConfig)
        {
            _config = config;
            _fireSystem = fireSystem;
            _resolver = resolver;
            _lightBudget = lightBudget;
            _visualConfig = visualConfig;
        }

        public UniTask CreateAsync(IReadOnlyList<FireSpotMarker> markers)
        {
            foreach (FireSpotMarker marker in markers)
            {
                FireSpot spot = new FireSpot(marker.Id, marker.SpotPosition, marker.Radius, marker.Height, marker.Material, marker.IsMovable);
                _fireSystem.Register(spot);

                FireSpotView view = _resolver.Instantiate(_config.FireSpotPrefab, marker.transform);
                view.Bind(spot);
                _lightBudget.Register(view);

                if (marker.BurnsOwnMeshes)
                    marker.gameObject.AddComponent<FireCharView>().Bind(spot, marker.CharredMeshes, _visualConfig);

                if (spot.IsMovable)
                    marker.gameObject.AddComponent<FireSpotFollower>().Bind(spot, marker);
            }

            return UniTask.CompletedTask;
        }
    }
}
