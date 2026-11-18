using _Project.Develop.Runtime.Gameplay.UI.Hud.Crosshair;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud
{
    public class HudView : MonoBehaviour
    {
        [SerializeField] private CrosshairView _crosshair;

        public CrosshairView Crosshair => _crosshair;
    }
}
