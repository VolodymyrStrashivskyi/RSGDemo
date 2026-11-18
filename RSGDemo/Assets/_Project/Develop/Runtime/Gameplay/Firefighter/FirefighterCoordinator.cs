using System;
using _Project.Develop.Runtime.Common.Input;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Firefighter
{
    public class FirefighterCoordinator : IFirefighterCoordinator, ITickable, ILateTickable, IDisposable
    {
        private readonly IInputService _inputService;

        private FirefighterBody _body;
        private FirstPersonCamera _camera;

        public FirefighterCoordinator(IInputService inputService) =>
            _inputService = inputService;

        public bool IsBound => _body != null && _camera != null;

        public void Bind(FirefighterBody body, FirstPersonCamera camera)
        {
            _body = body;
            _camera = camera;
            Synchronize();
        }

        public void Enable()
        {
            _inputService.EnablePlayerInput();
            SetCursorLocked(true);
        }

        public void Tick()
        {
            if (IsBound == false)
                return;

            _camera.Look(_inputService.Look);

            Vector2 moveInput = _inputService.Move;
            Vector3 direction = _camera.PlanarRotation * new Vector3(moveInput.x, 0f, moveInput.y);
            _body.Move(direction, _inputService.IsRunning);

            if (_inputService.IsJumpPressed)
                _body.Jump();
        }

        public void LateTick()
        {
            if (IsBound == false)
                return;

            Synchronize();
        }

        public void Dispose()
        {
            _inputService.DisablePlayerInput();
            SetCursorLocked(false);
        }

        private void Synchronize()
        {
            _body.SetYaw(_camera.Yaw);
            _camera.SetPosition(_body.HeadPoint.position);
        }

        private void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = isLocked == false;
        }
    }
}
