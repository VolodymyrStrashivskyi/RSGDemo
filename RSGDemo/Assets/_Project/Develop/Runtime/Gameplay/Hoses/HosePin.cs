using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Hoses
{
    public readonly struct HosePin
    {
        public HosePin(Vector3 position, Vector3 direction)
        {
            Position = position;
            Direction = direction.normalized;
        }

        public Vector3 Position { get; }

        public Vector3 Direction { get; }

        public static HosePin Lerp(HosePin from, HosePin to, float share) =>
            new HosePin(Vector3.Lerp(from.Position, to.Position, share), Vector3.Slerp(from.Direction, to.Direction, share));
    }
}
