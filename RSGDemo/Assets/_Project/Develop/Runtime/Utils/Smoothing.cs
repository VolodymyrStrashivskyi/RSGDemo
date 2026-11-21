using UnityEngine;

namespace _Project.Develop.Runtime.Utils
{
    public static class Smoothing
    {
        public static float GetBlend(float sharpness, float deltaTime) =>
            1f - Mathf.Exp(-sharpness * deltaTime);

        public static float GetDecay(float drag, float deltaTime) =>
            Mathf.Exp(-drag * deltaTime);
    }
}
