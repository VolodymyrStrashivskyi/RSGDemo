using VContainer;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Infrastructure.DI
{
    public class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameplayEntryPoint>();
        }
    }
}
