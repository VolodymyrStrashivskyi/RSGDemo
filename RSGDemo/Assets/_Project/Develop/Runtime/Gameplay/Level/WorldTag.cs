using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Level
{
    public class WorldTag : MonoBehaviour
    {
        [field: SerializeField] public Transform StreetPoint { get; private set; }
        [field: SerializeField] public Transform FireTruckPoint { get; private set; }
        [field: SerializeField] public Transform HydrantPoint { get; private set; }
        [field: SerializeField] public Transform PlayerPoint { get; private set; }
    }
}
