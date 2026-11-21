using _Project.Develop.Runtime.Gameplay.House;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Street
{
    public class StreetView : MonoBehaviour
    {
        [SerializeField] private HouseView _house;

        public HouseView House => _house;
    }
}
