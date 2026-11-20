using System;
using _Project.Develop.Runtime.Common.Input;
using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;
using _Project.Develop.Runtime.Gameplay.Firefighter.Interaction;
using _Project.Develop.Runtime.Gameplay.Interaction;
using UnityEngine;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.Firefighter
{
    public class FirefighterCoordinator : IFirefighterCoordinator, ITickable, ILateTickable, IDisposable
    {
        private readonly IInputService _inputService;
        private readonly IInteractionScanner _interactionScanner;
        private readonly Grabber _grabber;

        private FirefighterBody _body;
        private FirstPersonCamera _camera;

        public FirefighterCoordinator(IInputService inputService, IInteractionScanner interactionScanner, Grabber grabber)
        {
            _inputService = inputService;
            _interactionScanner = interactionScanner;
            _grabber = grabber;

            _grabber.Grabbed += OnGrabbed;
            _grabber.Released += OnReleased;
        }

        public bool IsBound => _body != null && _camera != null;

        public void Bind(FirefighterBody body, FirstPersonCamera camera)
        {
            _body = body;
            _camera = camera;
            _grabber.Bind(body.GrabAnchor);
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

            _interactionScanner.Scan(_camera.ViewRay);

            if (_inputService.IsInteractPressed)
                Interact();

            if (_grabber.Held is ITriggerable triggerable)
                triggerable.SetTrigger(_inputService.IsFireHeld);

            if (_grabber.Held is IAdjustable adjustable && _inputService.AdjustDirection != 0)
                adjustable.Adjust(_inputService.AdjustDirection);

            ApplyRecoil();
        }

        public void LateTick()
        {
            if (IsBound == false)
                return;

            Synchronize();

            if (_grabber.Held is IAimable aimable)
                aimable.Aim(_camera.ViewRay.direction);
        }

        public void Dispose()
        {
            _grabber.Grabbed -= OnGrabbed;
            _grabber.Released -= OnReleased;
            _inputService.DisablePlayerInput();
            SetCursorLocked(false);
        }

        private void Interact()
        {
            if (_interactionScanner.HasTarget)
                _interactionScanner.Current.Interact(_body.transform);
            else if (_grabber.IsHolding())
                _grabber.Release();
        }

        private void ApplyRecoil()
        {
            Vector3 force = _grabber.Held is IRecoiling recoiling ? recoiling.RecoilForce : Vector3.zero;
            _body.SetRecoilForce(force);
            _camera.SetRecoilForce(force.magnitude);
        }

        private void OnGrabbed(IGrabbable item)
        {
            if (IsBound == false)
                return;

            if (item is IMovementConstraint constraint)
                _body.AddConstraint(constraint);

            if (item is IHandGrips grips)
                _body.Hands.Hold(grips);

            if (item is IFirstPersonHeld firstPerson)
                _camera.Hold(firstPerson.ViewModel);
            else if (item is IHandGrips sharedGrips)
                _camera.Arms.Hold(sharedGrips);
        }

        private void OnReleased(IGrabbable item)
        {
            if (IsBound == false)
                return;

            if (item is IMovementConstraint constraint)
                _body.RemoveConstraint(constraint);

            if (item is IHandGrips)
                _body.Hands.Release();

            if (item is IFirstPersonHeld firstPerson)
                _camera.Release(firstPerson.ViewModel);
            else if (item is IHandGrips)
                _camera.Arms.Release();
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
