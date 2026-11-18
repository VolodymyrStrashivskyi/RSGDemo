using _Project.Develop.Runtime.Gameplay.Firefighter.Body;
using _Project.Develop.Runtime.Gameplay.Firefighter.Camera;

namespace _Project.Develop.Runtime.Gameplay.Firefighter
{
    public interface IFirefighterCoordinator
    {
        bool IsBound { get; }

        void Bind(FirefighterBody body, FirstPersonCamera camera);
        void Enable();
    }
}
