using UnityEngine;

namespace _Project.Develop.Runtime.Common.Input
{
    public interface IInputService
    {
        Vector2 Move { get; }
        Vector2 Look { get; }
        bool IsRunning { get; }
        bool IsJumpPressed { get; }
        bool IsInteractPressed { get; }
        string InteractKeyName { get; }
        bool IsFireHeld { get; }
        int AdjustDirection { get; }

        void EnablePlayerInput();
        void DisablePlayerInput();
    }
}
