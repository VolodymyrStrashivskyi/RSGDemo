using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public interface IWaterPushable
    {
        void AddWaterPush(Vector3 impulse);
    }
}
