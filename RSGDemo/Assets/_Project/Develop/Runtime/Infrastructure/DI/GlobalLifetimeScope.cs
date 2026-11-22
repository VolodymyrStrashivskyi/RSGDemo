using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Common.Input;
using _Project.Develop.Runtime.Common.Raycast;
using _Project.Develop.Runtime.Common.SceneManagement;
using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Infrastructure.DI
{
    public class GlobalLifetimeScope : LifetimeScope
    {
        [SerializeField] private AudioConfig _audioConfig;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_audioConfig);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IInputService, InputService>(Lifetime.Singleton);
            builder.Register<IRaycastService, RaycastService>(Lifetime.Singleton);
            builder.Register<IAudioService, AudioService>(Lifetime.Singleton);
        }
    }
}
