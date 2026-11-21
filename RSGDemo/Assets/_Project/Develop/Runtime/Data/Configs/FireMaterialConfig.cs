using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireMaterialConfig", menuName = "Configs/Fire Material Config")]
    public class FireMaterialConfig : ScriptableObject
    {
        [field: Header("Burning")]
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Heat of a fire right after it catches, share of full heat")]
        public float IgnitionHeat { get; private set; } = 0.35f;
        [field: SerializeField, Min(0f), Tooltip("How fast an unattended fire grows to full heat, share per second")]
        public float GrowthRate { get; private set; } = 0.03f;

        [field: SerializeField, Min(1f), Tooltip("Seconds of full-heat burning until everything here has burned away for good")]
        public float BurnDuration { get; private set; } = 180f;

        [field: Header("Water")]
        [field: SerializeField, Min(1f), Tooltip("Water that knocks a fully grown fire down to embers, L")]
        public float LitersToCool { get; private set; } = 150f;
        [field: SerializeField, Min(1f), Tooltip("Water that puts out the embers once the flames are gone, L")]
        public float LitersToDouse { get; private set; } = 40f;
        [field: SerializeField, Min(0f), Tooltip("Seconds without water after which smoldering embers flare up again")]
        public float ReigniteDelay { get; private set; } = 6f;
        [field: SerializeField, Range(0.05f, 1f), Tooltip("Heat of a fire that flared up again from embers, share of full heat")]
        public float ReigniteHeat { get; private set; } = 0.5f;

        [field: Header("Spread")]
        [field: SerializeField, Min(0f), Tooltip("How far this fire reaches unlit spots around it, m; 0 keeps it in place")]
        public float SpreadRadius { get; private set; } = 3f;
        [field: SerializeField, Min(0.1f), Tooltip("Seconds a full-heat fire needs to light a spot right next to it; farther spots take longer")]
        public float SpreadTime { get; private set; } = 30f;
        [field: SerializeField, Range(0f, 1f), Tooltip("Heat below which this fire does not light its neighbours, share of full heat")]
        public float SpreadMinHeat { get; private set; } = 0.5f;
    }
}
