using System;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireSpot
    {
        private const float FULL_HEAT = 1f;
        private const float FULL_EMBERS = 1f;
        private const float FULL_IGNITION = 1f;

        public event Action<FireSpot> Ignited;
        public event Action<FireSpot> Smoldered;
        public event Action<FireSpot> Extinguished;
        public event Action<FireSpot> Reignited;
        public event Action<FireSpot, float> Watered;

        private float _timeSinceWater;

        public FireSpot(string id, Vector3 position, float radius, float height, FireMaterialConfig material)
        {
            Id = id;
            Position = position;
            Radius = radius;
            Height = height;
            Material = material;
        }

        public string Id { get; }

        public Vector3 Position { get; }

        public float Radius { get; }

        public float Height { get; }

        public FireMaterialConfig Material { get; }

        public FireState State { get; private set; }

        public float Heat { get; private set; }

        public float Embers { get; private set; }

        public float IgnitionProgress { get; private set; }

        public bool IsBurning => State == FireState.Burning;

        public bool IsActive => State == FireState.Burning || State == FireState.Smoldering;

        public Vector3 Top => Position + Vector3.up * Height;

        public void Ignite()
        {
            if (State != FireState.Unlit)
                return;

            IgnitionProgress = 0f;
            StartBurning(Material.IgnitionHeat);
            Ignited?.Invoke(this);
        }

        public void AddIgnitionProgress(float amount)
        {
            if (State != FireState.Unlit)
                return;

            IgnitionProgress += amount;

            if (IgnitionProgress >= FULL_IGNITION)
                Ignite();
        }

        public void ApplyWater(float liters)
        {
            if (liters <= 0f)
                return;

            _timeSinceWater = 0f;

            if (IsActive)
                Watered?.Invoke(this, liters);

            switch (State)
            {
                case FireState.Unlit:
                    IgnitionProgress = Mathf.Max(0f, IgnitionProgress - liters / Material.LitersToCool);
                    break;

                case FireState.Burning:
                    Cool(liters);
                    break;

                case FireState.Smoldering:
                    Douse(liters);
                    break;
            }
        }

        public void Tick(float deltaTime)
        {
            _timeSinceWater += deltaTime;

            if (State == FireState.Burning)
                Heat = Mathf.MoveTowards(Heat, FULL_HEAT, Material.GrowthRate * deltaTime);
            else if (State == FireState.Smoldering && _timeSinceWater >= Material.ReigniteDelay)
                Reignite();
        }

        public float GetDistanceTo(Vector3 point)
        {
            float height = Mathf.Clamp(point.y - Position.y, 0f, Height);
            Vector3 closest = Position + Vector3.up * height;
            return Mathf.Max(0f, Vector3.Distance(point, closest) - Radius);
        }

        private void StartBurning(float heat)
        {
            State = FireState.Burning;
            Heat = heat;
            Embers = 0f;
            _timeSinceWater = 0f;
        }

        private void Cool(float liters)
        {
            float litersToEmbers = Heat * Material.LitersToCool;

            if (liters < litersToEmbers)
            {
                Heat -= liters / Material.LitersToCool;
                return;
            }

            Heat = 0f;
            Embers = FULL_EMBERS;
            State = FireState.Smoldering;
            Smoldered?.Invoke(this);
            Douse(liters - litersToEmbers);
        }

        private void Douse(float liters)
        {
            Embers -= liters / Material.LitersToDouse;

            if (Embers > 0f)
                return;

            Embers = 0f;
            State = FireState.Extinguished;
            Extinguished?.Invoke(this);
        }

        private void Reignite()
        {
            StartBurning(Material.ReigniteHeat);
            Reignited?.Invoke(this);
        }
    }
}
