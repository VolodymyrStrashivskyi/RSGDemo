using UnityEngine;

namespace _Project.Develop.Runtime.Utils
{
    public static class VectorExtensions
    {
        public static Vector3 WithoutHeight(this Vector3 vector) =>
            new Vector3(vector.x, 0f, vector.z);

        public static Vector2 ToGroundPlane(this Vector3 vector) =>
            new Vector2(vector.x, vector.z);
    }
}
