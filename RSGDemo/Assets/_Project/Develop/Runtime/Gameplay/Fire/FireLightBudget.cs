using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireLightBudget : IFireLightBudget, ITickable
    {
        private readonly FireVisualConfig _config;
        private readonly List<FireSpotView> _views = new List<FireSpotView>();
        private readonly List<FireSpotView> _chosen = new List<FireSpotView>();
        private readonly List<float> _chosenScores = new List<float>();
        private Transform _viewer;

        public FireLightBudget(FireVisualConfig config) =>
            _config = config;

        public void Register(FireSpotView view)
        {
            if (_views.Contains(view) == false)
                _views.Add(view);
        }

        public void SetViewer(Transform viewer) =>
            _viewer = viewer;

        public void Tick()
        {
            _chosen.Clear();
            _chosenScores.Clear();

            foreach (FireSpotView view in _views)
            {
                if (view == null)
                    continue;

                view.SetLightAllowed(false);
                float level = view.LightLevel;

                if (level > 0f)
                    TryChoose(view, GetScore(view, level));
            }

            foreach (FireSpotView view in _chosen)
                view.SetLightAllowed(true);
        }

        private float GetScore(FireSpotView view, float level)
        {
            if (_viewer == null)
                return level;

            float distanceSquared = (view.transform.position - _viewer.position).sqrMagnitude;
            return level / (1f + distanceSquared * _config.LightDistanceFalloff);
        }

        private void TryChoose(FireSpotView view, float score)
        {
            if (_chosen.Count < _config.MaxLights)
            {
                _chosen.Add(view);
                _chosenScores.Add(score);
                return;
            }

            int weakest = FindWeakest();

            if (weakest < 0 || _chosenScores[weakest] >= score)
                return;

            _chosen[weakest] = view;
            _chosenScores[weakest] = score;
        }

        private int FindWeakest()
        {
            int weakest = -1;

            for (int i = 0; i < _chosenScores.Count; i++)
            {
                if (weakest < 0 || _chosenScores[i] < _chosenScores[weakest])
                    weakest = i;
            }

            return weakest;
        }
    }
}
