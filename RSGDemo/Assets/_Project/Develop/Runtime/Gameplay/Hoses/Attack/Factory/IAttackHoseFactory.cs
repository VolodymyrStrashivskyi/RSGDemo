using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack.Factory
{
    public interface IAttackHoseFactory
    {
        UniTask<AttackHose> CreateAsync(HoseReel reel, Transform nozzleHook);
    }
}
