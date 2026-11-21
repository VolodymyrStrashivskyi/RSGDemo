using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public struct FireIgnition
    {
        [field: SerializeField, Tooltip("Name of the fire spot marker that catches")]
        public string SpotId { get; private set; }
        [field: SerializeField, Min(0f), Tooltip("Seconds after the level starts")]
        public float Delay { get; private set; }

        public FireIgnition(string spotId, float delay)
        {
            SpotId = spotId;
            Delay = delay;
        }
    }
}
