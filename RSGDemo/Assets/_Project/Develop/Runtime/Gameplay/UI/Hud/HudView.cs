using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using _Project.Develop.Runtime.Gameplay.UI.Hud.WaterStatus;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private CrosshairView _crosshair;
        [SerializeField] private WaterStatusView _waterStatus;

        public CrosshairView Crosshair => _crosshair;

        public WaterStatusView WaterStatus => _waterStatus;
    }
}
