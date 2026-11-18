using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Street.Factory
{
    public interface IStreetFactory
    {
        UniTask<GameObject> CreateAsync();
    }
}
