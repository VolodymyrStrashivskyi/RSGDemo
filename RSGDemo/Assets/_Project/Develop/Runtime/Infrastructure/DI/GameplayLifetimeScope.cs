using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Fire;
using _Project.Develop.Runtime.Gameplay.Fire.Factory;
using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Interaction;
using _Project.Develop.Runtime.Gameplay.Hoses.Attack.Factory;
using _Project.Develop.Runtime.Gameplay.Hoses.Supply.Factory;
using _Project.Develop.Runtime.Gameplay.Hydrant.Factory;
using _Project.Develop.Runtime.Gameplay.Interaction;
using _Project.Develop.Runtime.Gameplay.Level;
using _Project.Develop.Runtime.Gameplay.Street.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus;
using _Project.Develop.Runtime.Gameplay.WaterStream;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
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
        [SerializeField] private WaterSupplyConfig _waterSupplyConfig;
        [SerializeField] private SupplyHoseConfig _supplyHoseConfig;
        [SerializeField] private WaterStreamConfig _waterStreamConfig;
        [SerializeField] private AttackHoseConfig _attackHoseConfig;
        [SerializeField] private FireConfig _fireConfig;
        [SerializeField] private FireVisualConfig _fireVisualConfig;
        [SerializeField] private FireScenarioConfig _fireScenarioConfig;
        [SerializeField] private WorldTag _worldTag;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_factoryConfig);
            builder.RegisterInstance(_firefighterConfig);
            builder.RegisterInstance(_cameraConfig);
            builder.RegisterInstance(_interactionConfig);
            builder.RegisterInstance(_crosshairConfig);
            builder.RegisterInstance(_waterSupplyConfig);
            builder.RegisterInstance(_supplyHoseConfig);
            builder.RegisterInstance(_waterStreamConfig);
            builder.RegisterInstance(_attackHoseConfig);
            builder.RegisterInstance(_fireConfig);
            builder.RegisterInstance(_fireVisualConfig);
            builder.RegisterInstance(_fireScenarioConfig);
            builder.RegisterComponent(_worldTag);

            builder.Register<IStreetFactory, StreetFactory>(Lifetime.Singleton);
            builder.Register<IFireTruckFactory, FireTruckFactory>(Lifetime.Singleton);
            builder.Register<IHydrantFactory, HydrantFactory>(Lifetime.Singleton);
            builder.Register<ISupplyHoseFactory, SupplyHoseFactory>(Lifetime.Singleton);
            builder.Register<IAttackHoseFactory, AttackHoseFactory>(Lifetime.Singleton);
            builder.Register<IFirefighterFactory, FirefighterFactory>(Lifetime.Singleton);
            builder.Register<IFirstPersonCameraFactory, FirstPersonCameraFactory>(Lifetime.Singleton);
            builder.Register<IHudFactory, HudFactory>(Lifetime.Singleton);
            builder.Register<IFireFactory, FireFactory>(Lifetime.Singleton);

            builder.Register<HydrantSource>(Lifetime.Singleton);
            builder.Register<WaterTank>(Lifetime.Singleton);
            builder.Register<SupplyLine>(Lifetime.Singleton);
            builder.Register<Pump>(Lifetime.Singleton);
            builder.RegisterEntryPoint<WaterSupplyChain>().As<IWaterSupplyChain>();

            builder.Register<IWaterImpacts, WaterImpacts>(Lifetime.Singleton);
            builder.RegisterEntryPoint<FireSystem>().As<IFireSystem>();
            builder.Register<IFireOutbreak, FireOutbreak>(Lifetime.Singleton);

            builder.Register<Grabber>(Lifetime.Singleton);
            builder.Register<IInteractionScanner, InteractionScanner>(Lifetime.Singleton);
            builder.Register<CrosshairPresenter>(Lifetime.Singleton);
            builder.RegisterEntryPoint<WaterStatusPresenter>().AsSelf();

            builder.RegisterEntryPoint<FirefighterCoordinator>().As<IFirefighterCoordinator>();
        }
    }
}
