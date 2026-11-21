using System;
using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;

namespace _Project.Develop.Runtime.Data.Configs
{
    [Serializable]
    public struct InteractionActionLabel
    {
        [field: SerializeField] public InteractionAction Action { get; private set; }
        [field: SerializeField, Tooltip("Text shown next to the interact key")]
        public string Label { get; private set; }

        public InteractionActionLabel(InteractionAction action, string label)
        {
            Action = action;
            Label = label;
        }
    }
}
