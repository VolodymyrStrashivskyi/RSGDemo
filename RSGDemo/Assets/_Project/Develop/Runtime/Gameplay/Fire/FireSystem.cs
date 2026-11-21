using System;
using System.Collections.Generic;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterStream;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSystem : IFireSystem, ITickable, IDisposable
    {
        public event Action<FireSpot> SpotIgnited;
        public event Action<FireSpot> SpotExtinguished;
        public event Action<FireSpot> SpotReignited;
        public event Action AllExtinguished;

        private readonly List<FireSpot> _spots = new List<FireSpot>();
        private readonly List<FireLink> _links = new List<FireLink>();
        private readonly IWaterImpacts _waterImpacts;
        private readonly FireConfig _config;

        public FireSystem(IWaterImpacts waterImpacts, FireConfig config)
        {
            _waterImpacts = waterImpacts;
            _config = config;

            _waterImpacts.Landed += OnWaterLanded;
        }

        public IReadOnlyList<FireSpot> Spots => _spots;

        public int ActiveCount
        {
            get
            {
                int count = 0;

                foreach (FireSpot spot in _spots)
                {
                    if (spot.IsActive)
                        count++;
                }

                return count;
            }
        }

        public void Register(FireSpot spot)
        {
            if (_spots.Contains(spot))
                return;

            foreach (FireSpot other in _spots)
            {
                TryLink(other, spot);
                TryLink(spot, other);
            }

            _spots.Add(spot);
            spot.Ignited += OnSpotIgnited;
            spot.Extinguished += OnSpotExtinguished;
            spot.Reignited += OnSpotReignited;
        }

        public void Tick()
        {
            float deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
                return;

            foreach (FireSpot spot in _spots)
                spot.Tick(deltaTime);

            Spread(deltaTime);
        }

        public void Dispose()
        {
            _waterImpacts.Landed -= OnWaterLanded;

            foreach (FireSpot spot in _spots)
            {
                spot.Ignited -= OnSpotIgnited;
                spot.Extinguished -= OnSpotExtinguished;
                spot.Reignited -= OnSpotReignited;
            }
        }

        private void TryLink(FireSpot source, FireSpot target)
        {
            if (source.Material.SpreadRadius <= 0f)
                return;

            float reach = source.Material.SpreadRadius + target.Radius;
            float distance = Vector3.Distance(source.Position, target.Position);

            if (distance < reach)
                _links.Add(new FireLink(source, target, 1f - distance / reach));
        }

        private void Spread(float deltaTime)
        {
            foreach (FireLink link in _links)
            {
                FireSpot source = link.Source;

                if (source.IsBurning == false || source.Heat < source.Material.SpreadMinHeat || link.Target.State != FireState.Unlit)
                    continue;

                link.Target.AddIgnitionProgress(source.Heat * link.Closeness * deltaTime / source.Material.SpreadTime);
            }
        }

        private void OnWaterLanded(WaterImpact impact)
        {
            float reach = impact.Radius + _config.WaterReach;

            foreach (FireSpot spot in _spots)
            {
                if (spot.State == FireState.Extinguished)
                    continue;

                float gap = spot.GetDistanceTo(impact.Point);

                if (gap > reach)
                    continue;

                float falloff = gap - impact.Radius;
                float share = falloff > 0f && _config.WaterReach > 0f ? 1f - falloff / _config.WaterReach : 1f;
                spot.ApplyWater(impact.Liters * share);
            }
        }

        private void OnSpotIgnited(FireSpot spot) =>
            SpotIgnited?.Invoke(spot);

        private void OnSpotReignited(FireSpot spot) =>
            SpotReignited?.Invoke(spot);

        private void OnSpotExtinguished(FireSpot spot)
        {
            SpotExtinguished?.Invoke(spot);

            if (ActiveCount == 0)
                AllExtinguished?.Invoke();
        }
    }
}
