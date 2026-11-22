using _Project.Develop.Runtime.Gameplay.Fire;
using _Project.Develop.Runtime.Gameplay.Fire.Factory;
using _Project.Develop.Runtime.Gameplay.FireTruck;
using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
using _Project.Develop.Runtime.Gameplay.Hoses.Attack.Factory;
using _Project.Develop.Runtime.Gameplay.Hoses.Supply.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Dispatch;
using _Project.Develop.Runtime.Gameplay.UI.Hud.InteractionPrompt;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Factory;
using _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus;
using _Project.Develop.Runtime.Gameplay.Hydrant.Factory;
using _Project.Develop.Runtime.Gameplay.Street;
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
        private IAttackHoseFactory _attackHoseFactory;
        private IFirefighterFactory _firefighterFactory;
        private IFirstPersonCameraFactory _cameraFactory;
        private IFirefighterCoordinator _firefighterCoordinator;
        private IHudFactory _hudFactory;
        private IFireFactory _fireFactory;
        private IFireOutbreak _fireOutbreak;
        private IFireLightBudget _fireLightBudget;
        private IFireAudio _fireAudio;
        private IFireCharVolume _fireCharVolume;
        private CrosshairPresenter _crosshairPresenter;
        private InteractionPromptPresenter _interactionPromptPresenter;
        private WaterStatusPresenter _waterStatusPresenter;
        private DispatchPresenter _dispatchPresenter;

        [Inject]
        public void Construct(
            IStreetFactory streetFactory,
            IFireTruckFactory fireTruckFactory,
            IHydrantFactory hydrantFactory,
            ISupplyHoseFactory supplyHoseFactory,
            IAttackHoseFactory attackHoseFactory,
            IFirefighterFactory firefighterFactory,
            IFirstPersonCameraFactory cameraFactory,
            IFirefighterCoordinator firefighterCoordinator,
            IHudFactory hudFactory,
            IFireFactory fireFactory,
            IFireOutbreak fireOutbreak,
            IFireLightBudget fireLightBudget,
            IFireAudio fireAudio,
            IFireCharVolume fireCharVolume,
            CrosshairPresenter crosshairPresenter,
            InteractionPromptPresenter interactionPromptPresenter,
            WaterStatusPresenter waterStatusPresenter,
            DispatchPresenter dispatchPresenter)
        {
            _streetFactory = streetFactory;
            _fireTruckFactory = fireTruckFactory;
            _hydrantFactory = hydrantFactory;
            _supplyHoseFactory = supplyHoseFactory;
            _attackHoseFactory = attackHoseFactory;
            _firefighterFactory = firefighterFactory;
            _cameraFactory = cameraFactory;
            _firefighterCoordinator = firefighterCoordinator;
            _hudFactory = hudFactory;
            _fireFactory = fireFactory;
            _fireOutbreak = fireOutbreak;
            _fireLightBudget = fireLightBudget;
            _fireAudio = fireAudio;
            _fireCharVolume = fireCharVolume;
            _crosshairPresenter = crosshairPresenter;
            _interactionPromptPresenter = interactionPromptPresenter;
            _waterStatusPresenter = waterStatusPresenter;
            _dispatchPresenter = dispatchPresenter;
        }

        public void Start() =>
            BuildLevelAsync().Forget();

        private async UniTaskVoid BuildLevelAsync()
        {
            StreetView street = await _streetFactory.CreateAsync();
            await _fireFactory.CreateAsync(street.House.FireSpots);
            _fireCharVolume.Build(street.House.transform);
            FireTruckView truck = await _fireTruckFactory.CreateAsync();
            await _hydrantFactory.CreateAsync();
            await _supplyHoseFactory.CreateAsync(truck.IntakePoint, truck.HoseLayPoint);
            await _attackHoseFactory.CreateAsync(truck.HoseReel, truck.NozzleHookPoint);

            FirefighterBody body = await _firefighterFactory.CreateAsync();
            FirstPersonCamera camera = await _cameraFactory.CreateAsync(body.HeadPoint);
            _firefighterCoordinator.Bind(body, camera);
            _fireLightBudget.SetViewer(camera.transform);
            _fireAudio.SetListener(camera.transform);

            HudView hud = await _hudFactory.CreateAsync();
            _crosshairPresenter.Bind(hud.Crosshair);
            _interactionPromptPresenter.Bind(hud.InteractionPrompt);
            _waterStatusPresenter.Bind(hud.WaterStatus);
            _dispatchPresenter.Bind(hud.Dispatch);

            _firefighterCoordinator.Enable();
            _fireOutbreak.StartAsync(destroyCancellationToken).Forget();
        }
    }
}
