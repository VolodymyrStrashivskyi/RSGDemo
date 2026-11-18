using _Project.Develop.Runtime.Common.Input;
using _Project.Develop.Runtime.Common.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Infrastructure.DI
{
    public class GlobalLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IInputService, InputService>(Lifetime.Singleton);
        }
    }
}
