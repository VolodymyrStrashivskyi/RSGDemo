using System.Collections.Generic;
using _Project.Develop.Runtime.Gameplay.Fire;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.House
{
    public class HouseView : MonoBehaviour
    {
        [SerializeField, Tooltip("Places in and on the house that can burn")]
        private List<FireSpotMarker> _fireSpots = new List<FireSpotMarker>();

        public IReadOnlyList<FireSpotMarker> FireSpots => _fireSpots;
    }
}
