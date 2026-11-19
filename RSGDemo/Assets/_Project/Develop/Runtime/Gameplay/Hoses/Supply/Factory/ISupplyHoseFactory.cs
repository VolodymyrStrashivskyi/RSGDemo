using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Supply.Factory
{
    public interface ISupplyHoseFactory
    {
        UniTask<SupplyHose> CreateAsync(Transform intakePoint, Transform layPoint);
    }
}
