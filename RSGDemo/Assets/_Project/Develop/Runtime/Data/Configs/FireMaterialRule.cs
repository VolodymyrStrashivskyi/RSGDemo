using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public struct FireMaterialRule
    {
        [field: SerializeField, Tooltip("Objects whose name starts with this burn as the material below")]
        public string NamePrefix { get; private set; }
        [field: SerializeField, Tooltip("What the matching objects are made of")]
        public FireMaterialConfig Material { get; private set; }
    }
}
