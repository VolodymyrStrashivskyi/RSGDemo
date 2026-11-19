using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Firefighter.Body
{
    public interface IMovementConstraint
    {
        Vector3 Constrain(Vector3 currentPosition, Vector3 desiredPosition);
    }
}
