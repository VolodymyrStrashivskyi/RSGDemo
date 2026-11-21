using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public readonly struct WaterImpact
    {
        public readonly Vector3 Point;
        public readonly float Liters;
        public readonly float Radius;

        public WaterImpact(Vector3 point, float liters, float radius)
        {
            Point = point;
            Liters = liters;
            Radius = radius;
        }
    }
}
