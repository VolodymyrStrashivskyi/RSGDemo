using _Project.Develop.Runtime.Gameplay.FireTruck.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory;
using _Project.Develop.Runtime.Gameplay.Firefighter.Factory;
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
        private IFirefighterFactory _firefighterFactory;
        private IFirstPersonCameraFactory _cameraFactory;
        private IFirefighterCoordinator _firefighterCoordinator;

        [Inject]
        public void Construct(
            IStreetFactory streetFactory,
            IFireTruckFactory fireTruckFactory,
            IHydrantFactory hydrantFactory,
            IFirefighterFactory firefighterFactory,
            IFirstPersonCameraFactory cameraFactory,
            IFirefighterCoordinator firefighterCoordinator)
        {
            _streetFactory = streetFactory;
            _fireTruckFactory = fireTruckFactory;
            _hydrantFactory = hydrantFactory;
            _firefighterFactory = firefighterFactory;
            _cameraFactory = cameraFactory;
            _firefighterCoordinator = firefighterCoordinator;
        }

        public void Start() =>
            BuildLevelAsync().Forget();

        private async UniTaskVoid BuildLevelAsync()
        {
            await _streetFactory.CreateAsync();
            await _fireTruckFactory.CreateAsync();
            await _hydrantFactory.CreateAsync();

            FirefighterBody body = await _firefighterFactory.CreateAsync();
            FirstPersonCamera camera = await _cameraFactory.CreateAsync(body.HeadPoint);
            _firefighterCoordinator.Bind(body, camera);
            _firefighterCoordinator.Enable();
        }
    }
}
