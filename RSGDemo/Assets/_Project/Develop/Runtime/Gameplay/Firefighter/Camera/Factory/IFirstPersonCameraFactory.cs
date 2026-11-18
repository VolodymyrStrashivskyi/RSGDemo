using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Camera.Factory
{
    public interface IFirstPersonCameraFactory
    {
        UniTask<FirstPersonCamera> CreateAsync(Transform headPoint);
    }
}
