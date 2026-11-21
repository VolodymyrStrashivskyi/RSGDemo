using System.Collections.Generic;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [CreateAssetMenu(fileName = "FireSpotLayoutConfig", menuName = "Configs/Fire Spot Layout Config")]
    public class FireSpotLayoutConfig : ScriptableObject
    {
        [field: Header("Props")]
        [field: SerializeField, Tooltip("Which props burn and what they are made of; props matching no rule do not burn")]
        public List<FireMaterialRule> PropRules { get; private set; } = new List<FireMaterialRule>();
        [field: SerializeField, Range(0.1f, 1f), Tooltip("Spot radius as a share of the prop's average half width")]
        public float PropRadiusShare { get; private set; } = 0.8f;
        [field: SerializeField, Min(0.05f), Tooltip("Smallest spot radius on a prop, m")]
        public float PropMinRadius { get; private set; } = 0.2f;
        [field: SerializeField, Min(0.05f), Tooltip("Largest spot radius on a prop, m")]
        public float PropMaxRadius { get; private set; } = 0.9f;
        [field: SerializeField, Min(0.1f), Tooltip("Lowest flames on a prop, m")]
        public float PropMinHeight { get; private set; } = 0.4f;
        [field: SerializeField, Min(0.1f), Tooltip("Highest flames on a prop, m")]
        public float PropMaxHeight { get; private set; } = 2f;

        [field: Header("Structure")]
        [field: SerializeField, Tooltip("What walls, floors, facades and the roof are made of")]
        public FireMaterialConfig StructureMaterial { get; private set; }
        [field: SerializeField, Tooltip("Place spots along the inner walls of every storey")]
        public bool PlaceWalls { get; private set; } = true;
        [field: SerializeField, Min(0.5f), Tooltip("Distance between spots along inner walls, m")]
        public float WallSpacing { get; private set; } = 2.4f;
        [field: SerializeField, Min(0.05f), Tooltip("Radius of a wall spot, m")]
        public float WallRadius { get; private set; } = 0.5f;
        [field: SerializeField, Tooltip("Place spots in front of the outer walls")]
        public bool PlaceFacades { get; private set; } = true;
        [field: SerializeField, Min(0.5f), Tooltip("Distance between spots along outer walls, m")]
        public float FacadeSpacing { get; private set; } = 3f;
        [field: SerializeField, Min(0.05f), Tooltip("Radius of a facade spot, m")]
        public float FacadeRadius { get; private set; } = 0.5f;
        [field: SerializeField, Tooltip("Place spots on the roof slopes")]
        public bool PlaceRoof { get; private set; } = true;
        [field: SerializeField, Min(0.5f), Tooltip("Distance between spots on the roof, m")]
        public float RoofSpacing { get; private set; } = 3f;
        [field: SerializeField, Min(0.05f), Tooltip("Radius of a roof spot, m")]
        public float RoofRadius { get; private set; } = 0.9f;
        [field: SerializeField, Min(0.1f), Tooltip("Height of the flames on the roof, m")]
        public float RoofFlameHeight { get; private set; } = 0.8f;
    }
}
