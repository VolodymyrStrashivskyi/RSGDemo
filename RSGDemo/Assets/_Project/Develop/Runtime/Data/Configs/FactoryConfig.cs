using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.UI.Hud;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FactoryConfig", menuName = "Configs/Factory Config")]
    public class FactoryConfig : ScriptableObject
    {
        [field: SerializeField] public GameObject StreetPrefab { get; private set; }
        [field: SerializeField] public GameObject FireTruckPrefab { get; private set; }
        [field: SerializeField] public GameObject HydrantPrefab { get; private set; }
        [field: SerializeField] public FirefighterBody FirefighterPrefab { get; private set; }
        [field: SerializeField] public FirstPersonCamera CameraPrefab { get; private set; }
        [field: SerializeField] public HudView HudPrefab { get; private set; }
    }
}
