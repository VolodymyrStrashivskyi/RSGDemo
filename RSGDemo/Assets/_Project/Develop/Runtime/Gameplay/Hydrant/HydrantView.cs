using _Project.Develop.Runtime.Gameplay.Hoses;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hydrant
{
    public class HydrantView : MonoBehaviour
    {
        [SerializeField] private HoseSocket _socket;
        [SerializeField] private HydrantValve _valve;

        public HoseSocket Socket => _socket;

        public HydrantValve Valve => _valve;
    }
}
