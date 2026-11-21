using System.Threading;
using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public interface IFireOutbreak
    {
        UniTask StartAsync(CancellationToken cancellationToken);
    }
}
