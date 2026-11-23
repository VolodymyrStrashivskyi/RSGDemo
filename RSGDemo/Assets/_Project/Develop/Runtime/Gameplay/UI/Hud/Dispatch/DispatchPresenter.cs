using System;
using System.Globalization;
using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.Fire;
using VContainer.Unity;

namespace _Project.Develop.Runtime.Gameplay.UI.Hud.Dispatch
{
    public class DispatchPresenter : ITickable, IDisposable
    {
        private const string COUNT_FORMAT = "{0}<size=65%><alpha=#8C>{1}";
        private const int NOT_SHOWN = -1;

        private readonly IFireSystem _fireSystem;
        private readonly DispatchConfig _config;
        private readonly IAudioService _audioService;

        private DispatchView _view;
        private bool _hasFireStarted;
        private int _shownCount = NOT_SHOWN;

        public DispatchPresenter(IFireSystem fireSystem, DispatchConfig config, IAudioService audioService)
        {
            _audioService = audioService;
            _fireSystem = fireSystem;
            _config = config;
        }

        public void Bind(DispatchView view)
        {
            _view = view;
            _view.Setup(_config.CallLabel, _config.Title, _config.Address);
            _view.ShowProgress(_config.Objective, string.Empty);
            _view.SlideIn();

            _fireSystem.SpotIgnited += OnSpotIgnited;
            _fireSystem.SpotReignited += OnSpotFlaredUp;
            _fireSystem.SpotBurnedOut += OnSpotFlaredUp;
        }

        public void Tick()
        {
            if (_view == null || _hasFireStarted == false)
                return;

            int activeCount = _fireSystem.ActiveCount;

            if (activeCount == _shownCount)
                return;

            _shownCount = activeCount;

            if (activeCount == 0)
            {
                _view.ShowCompleted(_config.CompletedLabel);
                _audioService.Play(_config.CompletedSound);
            }
            else
                _view.ShowProgress(_config.Objective, string.Format(CultureInfo.InvariantCulture, COUNT_FORMAT, activeCount, _config.BurningSuffix));
        }

        public void Dispose()
        {
            _fireSystem.SpotIgnited -= OnSpotIgnited;
            _fireSystem.SpotReignited -= OnSpotFlaredUp;
            _fireSystem.SpotBurnedOut -= OnSpotFlaredUp;
        }

        private void OnSpotIgnited(FireSpot spot)
        {
            if (_hasFireStarted)
                return;

            _hasFireStarted = true;
            _view.Alert();
            _audioService.Play(_config.AlertSound);
        }

        private void OnSpotFlaredUp(FireSpot spot) =>
            _view.Alert();
    }
}
