using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hydrant.Factory
{
    public interface IHydrantFactory
    {
        UniTask<GameObject> CreateAsync();
    }
}
