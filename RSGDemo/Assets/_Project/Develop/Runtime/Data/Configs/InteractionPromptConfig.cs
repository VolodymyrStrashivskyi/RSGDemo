using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "InteractionPromptConfig", menuName = "Configs/Interaction Prompt Config")]
    public class InteractionPromptConfig : ScriptableObject
    {
        [field: SerializeField, Tooltip("Text under the crosshair for each interaction")]
        public InteractionActionLabel[] Labels { get; private set; } =
        {
            new InteractionActionLabel(InteractionAction.PickUp, "Pick up"),
            new InteractionActionLabel(InteractionAction.Open, "Open"),
            new InteractionActionLabel(InteractionAction.Close, "Close"),
            new InteractionActionLabel(InteractionAction.OpenValve, "Open valve"),
            new InteractionActionLabel(InteractionAction.CloseValve, "Close valve"),
            new InteractionActionLabel(InteractionAction.ConnectHose, "Connect hose")
        };
    }
}
