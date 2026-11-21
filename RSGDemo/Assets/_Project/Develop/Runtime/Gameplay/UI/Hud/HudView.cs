using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.Dispatch;
using _Project.Develop.Runtime.Gameplay.UI.Hud.InteractionPrompt;
using _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private CrosshairView _crosshair;
        [SerializeField] private InteractionPromptView _interactionPrompt;
        [SerializeField] private WaterStatusView _waterStatus;
        [SerializeField] private DispatchView _dispatch;

        public CrosshairView Crosshair => _crosshair;

        public InteractionPromptView InteractionPrompt => _interactionPrompt;

        public WaterStatusView WaterStatus => _waterStatus;

        public DispatchView Dispatch => _dispatch;
    }
}
