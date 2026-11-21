using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace _Project.Develop.Runtime.Gameplay.Fire.Factory
{
    public interface IFireFactory
    {
        UniTask CreateAsync(IReadOnlyList<FireSpotMarker> markers);
    }
}
