using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.FireTruck.Factory
{
    public interface IFireTruckFactory
    {
        UniTask<GameObject> CreateAsync();
    }
}
