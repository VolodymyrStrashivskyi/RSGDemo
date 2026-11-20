using System;
using UnityEngine;

namespace _Project.Develop.Runtime.Common.Input
{
    public class InputService : IInputService, IDisposable
    {
        private readonly GameInput _gameInput;

        public InputService() =>
            _gameInput = new GameInput();

        public Vector2 Move => _gameInput.Player.Move.ReadValue<Vector2>();

        public Vector2 Look => _gameInput.Player.Look.ReadValue<Vector2>();

        public bool IsRunning => _gameInput.Player.Run.IsPressed();

        public bool IsJumpPressed => _gameInput.Player.Jump.WasPressedThisFrame();

        public bool IsInteractPressed => _gameInput.Player.Interact.WasPressedThisFrame();

        public bool IsFireHeld => _gameInput.Player.Fire.IsPressed();

        public void EnablePlayerInput() =>
            _gameInput.Player.Enable();

        public void DisablePlayerInput() =>
            _gameInput.Player.Disable();

        public void Dispose() =>
            _gameInput.Dispose();
    }
}
