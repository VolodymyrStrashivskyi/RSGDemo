using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Interaction;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Factory;
using _Project.Develop.Runtime.Gameplay.Hydrant.Factory;
using _Project.Develop.Runtime.Gameplay.Level;
using _Project.Develop.Runtime.Gameplay.Street.Factory;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Infrastructure.DI
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private FactoryConfig _factoryConfig;
        [SerializeField] private FirefighterConfig _firefighterConfig;
        [SerializeField] private CameraConfig _cameraConfig;
        [SerializeField] private InteractionConfig _interactionConfig;
        [SerializeField] private CrosshairConfig _crosshairConfig;
        [SerializeField] private WorldTag _worldTag;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_factoryConfig);
            builder.RegisterInstance(_firefighterConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_interactionConfig);
            builder.RegisterInstance(_crosshairConfig);
            builder.RegisterComponent(_worldTag);

            builder.Register<IStreetFactory, StreetFactory>(Lifetime.Singleton);
            builder.Register<IFireTruckFactory, FireTruckFactory>(Lifetime.Singleton);
            builder.Register<IHydrantFactory, HydrantFactory>(Lifetime.Singleton);
            builder.Register<IFirefighterFactory, FirefighterFactory>(Lifetime.Singleton);
            builder.Register<IFirstPersonCameraFactory, FirstPersonCameraFactory>(Lifetime.Singleton);
            builder.Register<IHudFactory, HudFactory>(Lifetime.Singleton);

            builder.Register<IInteractionScanner, InteractionScanner>(Lifetime.Singleton);
            builder.Register<CrosshairPresenter>(Lifetime.Singleton);

            builder.RegisterEntryPoint<FirefighterCoordinator>().As<IFirefighterCoordinator>();
        }
    }
}
