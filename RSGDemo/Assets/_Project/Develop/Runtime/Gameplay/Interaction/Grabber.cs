using System;

namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public class Grabber
    {
        public event Action<IGrabbable> Grabbed;
        public event Action<IGrabbable> Released;

        private GrabAnchor _anchor;

        public IGrabbable Held { get; private set; }

        public void Bind(GrabAnchor anchor) =>
            _anchor = anchor;

        public bool IsHolding() =>
            Held != null;

        public bool IsHolding<T>() where T : class, IGrabbable =>
            Held is T;

        public bool TryGetHeld<T>(out T item) where T : class, IGrabbable
        {
            item = Held as T;
            return item != null;
        }

        public bool TryGrab(IGrabbable item)
        {
            if (IsHolding() || _anchor == null)
                return false;

            Held = item;
            item.OnGrabbed(_anchor);
            Grabbed?.Invoke(item);
            return true;
        }

        public void Release()
        {
            if (IsHolding() == false)
                return;

            IGrabbable item = Held;
            Held = null;
            item.OnReleased();
            Released?.Invoke(item);
        }
    }
}
