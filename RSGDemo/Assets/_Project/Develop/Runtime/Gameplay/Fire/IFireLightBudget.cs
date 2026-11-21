using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public interface IFireLightBudget
    {
        void Register(FireSpotView view);
        void SetViewer(Transform viewer);
    }
}
