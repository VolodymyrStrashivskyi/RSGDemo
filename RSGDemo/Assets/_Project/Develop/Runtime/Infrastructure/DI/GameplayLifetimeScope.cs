using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
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
        [SerializeField] private WorldTag _worldTag;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_factoryConfig);
            builder.RegisterInstance(_firefighterConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterComponent(_worldTag);

            builder.Register<IStreetFactory, StreetFactory>(Lifetime.Singleton);
            builder.Register<IFireTruckFactory, FireTruckFactory>(Lifetime.Singleton);
            builder.Register<IHydrantFactory, HydrantFactory>(Lifetime.Singleton);
            builder.Register<IFirefighterFactory, FirefighterFactory>(Lifetime.Singleton);
            builder.Register<IFirstPersonCameraFactory, FirstPersonCameraFactory>(Lifetime.Singleton);

            builder.RegisterEntryPoint<FirefighterCoordinator>().As<IFirefighterCoordinator>();
        }
    }
}
