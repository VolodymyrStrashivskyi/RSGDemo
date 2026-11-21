using System;
using System.Collections.Generic;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public interface IFireSystem
    {
        event Action<FireSpot> SpotIgnited;
        event Action<FireSpot> SpotExtinguished;
        event Action<FireSpot> SpotReignited;
        event Action AllExtinguished;

        IReadOnlyList<FireSpot> Spots { get; }
        int ActiveCount { get; }

        void Register(FireSpot spot);
    }
}
