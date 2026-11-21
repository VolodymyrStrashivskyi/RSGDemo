using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public static class FireCharLook
    {
        public static float GetShownChar(FireSpot spot, FireVisualConfig config) =>
            1f - Mathf.Pow(1f - spot.Char, config.CharGain);

        public static float GetReach(FireSpot spot, FireVisualConfig config) =>
            config.CharReach + config.CharReachPerRadius * spot.Radius;
    }
}
