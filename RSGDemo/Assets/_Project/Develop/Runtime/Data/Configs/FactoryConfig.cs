using _Project.Develop.Runtime.Gameplay.FireTruck;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.Hoses.Attack;
using _Project.Develop.Runtime.Gameplay.Hoses.Supply;
using _Project.Develop.Runtime.Gameplay.Hydrant;
using _Project.Develop.Runtime.Gameplay.UI.Hud;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FactoryConfig", menuName = "Configs/Factory Config")]
    public class FactoryConfig : ScriptableObject
    {
        [field: SerializeField] public GameObject StreetPrefab { get; private set; }
        [field: SerializeField] public FireTruckView FireTruckPrefab { get; private set; }
        [field: SerializeField] public HydrantView HydrantPrefab { get; private set; }
        [field: SerializeField] public SupplyHose SupplyHosePrefab { get; private set; }
        [field: SerializeField] public AttackHose AttackHosePrefab { get; private set; }
        [field: SerializeField] public FirefighterBody FirefighterPrefab { get; private set; }
        [field: SerializeField] public FirstPersonCamera CameraPrefab { get; private set; }
        [field: SerializeField] public HudView HudPrefab { get; private set; }
    }
}
