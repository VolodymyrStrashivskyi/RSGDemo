namespace _Project.Develop.Runtime.Gameplay.Interaction
{
    public interface IGrabbable
    {
        void OnGrabbed(GrabAnchor anchor);
        void OnReleased();
    }
}
