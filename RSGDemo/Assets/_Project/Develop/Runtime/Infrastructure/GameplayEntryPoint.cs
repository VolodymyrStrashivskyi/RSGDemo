using _Project.Develop.Runtime.Gameplay.FireTruck;
using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
using _Project.Develop.Runtime.Gameplay.Hoses.Supply.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus;
using _Project.Develop.Runtime.Gameplay.Hydrant.Factory;
using _Project.Develop.Runtime.Gameplay.Street.Factory;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Infrastructure
{
    public class GameplayEntryPoint : MonoBehaviour
    {
        private IStreetFactory _streetFactory;
        private IFireTruckFactory _fireTruckFactory;
        private IHydrantFactory _hydrantFactory;
        private ISupplyHoseFactory _supplyHoseFactory;
        private IFirefighterFactory _firefighterFactory;
        private IFirstPersonCameraFactory _cameraFactory;
        private IFirefighterCoordinator _firefighterCoordinator;
        private IHudFactory _hudFactory;
        private CrosshairPresenter _crosshairPresenter;
        private WaterStatusPresenter _waterStatusPresenter;

        [Inject]
        public void Construct(
            IStreetFactory streetFactory,
            IFireTruckFactory fireTruckFactory,
            IHydrantFactory hydrantFactory,
            ISupplyHoseFactory supplyHoseFactory,
            IFirefighterFactory firefighterFactory,
            IFirstPersonCameraFactory cameraFactory,
            IFirefighterCoordinator firefighterCoordinator,
            IHudFactory hudFactory,
            CrosshairPresenter crosshairPresenter,
            WaterStatusPresenter waterStatusPresenter)
        {
            _streetFactory = streetFactory;
            _fireTruckFactory = fireTruckFactory;
            _hydrantFactory = hydrantFactory;
            _supplyHoseFactory = supplyHoseFactory;
            _firefighterFactory = firefighterFactory;
            _cameraFactory = cameraFactory;
            _firefighterCoordinator = firefighterCoordinator;
            _hudFactory = hudFactory;
            _crosshairPresenter = crosshairPresenter;
            _waterStatusPresenter = waterStatusPresenter;
        }

        public void Start() =>
            BuildLevelAsync().Forget();

        private async UniTaskVoid BuildLevelAsync()
        {
            await _streetFactory.CreateAsync();
            FireTruckView truck = await _fireTruckFactory.CreateAsync();
            await _hydrantFactory.CreateAsync();
            await _supplyHoseFactory.CreateAsync(truck.IntakePoint, truck.HoseLayPoint);

            FirefighterBody body = await _firefighterFactory.CreateAsync();
            FirstPersonCamera camera = await _cameraFactory.CreateAsync(body.HeadPoint);
            _firefighterCoordinator.Bind(body, camera);

            HudView hud = await _hudFactory.CreateAsync();
            _crosshairPresenter.Bind(hud.Crosshair);
            _waterStatusPresenter.Bind(hud.WaterStatus);

            _firefighterCoordinator.Enable();
        }
    }
}
