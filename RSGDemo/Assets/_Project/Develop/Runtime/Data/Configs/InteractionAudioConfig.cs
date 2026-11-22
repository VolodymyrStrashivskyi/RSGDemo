using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "InteractionAudioConfig", menuName = "Configs/Audio/Interaction Audio Config")]
    public class InteractionAudioConfig : ScriptableObject
    {
        [field: SerializeField] public SoundCue PickUp { get; private set; }
        [field: SerializeField] public SoundCue Drop { get; private set; }
        [field: SerializeField, Tooltip("Hose coupling locks onto a socket")]
        public SoundCue Connect { get; private set; }
        [field: SerializeField, Tooltip("The reel jerks when the hose runs out")]
        public SoundCue ReelBrake { get; private set; }
        [field: SerializeField] public SoundCue Valve { get; private set; }
        [field: SerializeField] public SoundCue Door { get; private set; }
    }
}
