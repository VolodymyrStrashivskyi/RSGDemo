using System;
using System.Collections.Generic;
using System.Threading;
using _Project.Develop.Runtime.Data.Configs;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireOutbreak : IFireOutbreak
    {
        private readonly IFireSystem _fireSystem;
        private readonly FireScenarioConfig _scenario;

        public FireOutbreak(IFireSystem fireSystem, FireScenarioConfig scenario)
        {
            _fireSystem = fireSystem;
            _scenario = scenario;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            List<FireIgnition> ignitions = new List<FireIgnition>(_scenario.Ignitions);
            ignitions.Sort((first, second) => first.Delay.CompareTo(second.Delay));
            float elapsed = 0f;

            foreach (FireIgnition ignition in ignitions)
            {
                float wait = ignition.Delay - elapsed;

                if (wait > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(wait), cancellationToken: cancellationToken);

                elapsed = Mathf.Max(elapsed, ignition.Delay);
                Ignite(ignition.SpotId);
            }
        }

        private void Ignite(string spotId)
        {
            foreach (FireSpot spot in _fireSystem.Spots)
            {
                if (spot.Id == spotId)
                {
                    spot.Ignite();
                    return;
                }
            }

            Debug.LogWarning($"Fire scenario: no fire spot named '{spotId}'");
        }
    }
}
