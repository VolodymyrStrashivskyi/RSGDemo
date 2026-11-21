using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireScenarioConfig", menuName = "Configs/Fire Scenario Config")]
    public class FireScenarioConfig : ScriptableObject
    {
        [SerializeField, Tooltip("Spots that catch fire on their own, in any order; the rest only catch from their neighbours")]
        private List<FireIgnition> _ignitions = new List<FireIgnition>();

        public IReadOnlyList<FireIgnition> Ignitions => _ignitions;
    }
}
