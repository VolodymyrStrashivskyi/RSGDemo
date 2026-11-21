using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireConfig", menuName = "Configs/Fire Config")]
    public class FireConfig : ScriptableObject
    {
        [field: SerializeField, Min(0f), Tooltip("Extra distance around the water impact that still reaches a fire, m")]
        public float WaterReach { get; private set; } = 0.3f;
        [field: SerializeField, Min(0.05f), Tooltip("How far a movable burning thing has to travel before its neighbours are found again, m")]
        public float RelinkDistance { get; private set; } = 0.5f;
    }
}
