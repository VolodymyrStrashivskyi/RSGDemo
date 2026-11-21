using System;
using System.Collections.Generic;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public interface IFireSystem
    {
        event Action<FireSpot> SpotIgnited;
        event Action<FireSpot> SpotExtinguished;
        event Action<FireSpot> SpotReignited;
        event Action<FireSpot> SpotBurnedOut;
        event Action AllExtinguished;

        IReadOnlyList<FireSpot> Spots { get; }
        int ActiveCount { get; }
        int BurnedOutCount { get; }

        void Register(FireSpot spot);
    }
}
