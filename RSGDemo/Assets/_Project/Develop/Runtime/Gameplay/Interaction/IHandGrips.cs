using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public interface IHandGrips
    {
        Transform LeftHandGrip { get; }

        Transform RightHandGrip { get; }
    }
}
