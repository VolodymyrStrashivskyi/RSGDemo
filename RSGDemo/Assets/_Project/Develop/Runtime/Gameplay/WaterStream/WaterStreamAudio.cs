using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Utils;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.WaterStream
{
    public class WaterStreamAudio : MonoBehaviour
    {
        [SerializeField] private WaterStreamView _stream;
        [SerializeField] private WaterStreamAudioConfig _config;

        private IAudioService _audioService;
        private SoundLoop _outlet;
        private SoundLoop _impact;
        private float _outletVolume;
        private float _impactVolume;
        private bool _isOpen;

        [Inject]
        public void Construct(IAudioService audioService)
        {
            _audioService = audioService;
            _outlet = audioService.StartLoop(_config.Outlet);
            _impact = audioService.StartLoop(_config.Impact);
        }

        private void LateUpdate()
        {
            if (_outlet == null)
                return;

            float flow = _stream.Flow;
            NotifyIfOpened(flow);

            float blend = Smoothing.GetBlend(_config.Sharpness, Time.deltaTime);
            float loudness = Mathf.Clamp01(flow);
            _outletVolume = Mathf.Lerp(_outletVolume, loudness, blend);
            _impactVolume = Mathf.Lerp(_impactVolume, _stream.HasImpact ? loudness : 0f, blend);

            _outlet.SetPosition(transform.position);
            _outlet.SetPitch(Mathf.Lerp(_config.PitchByFlow.x, _config.PitchByFlow.y, flow));
            _outlet.SetVolume(_outletVolume);

            if (_stream.HasImpact)
                _impact.SetPosition(_stream.ImpactPoint);

            _impact.SetVolume(_impactVolume);
        }

        private void OnDestroy()
        {
            _outlet?.Release();
            _impact?.Release();
        }

        private void NotifyIfOpened(float flow)
        {
            bool isOpen = flow > _config.OpenFlow;

            if (isOpen && _isOpen == false)
                _audioService.Play(_config.Opened, transform.position);

            _isOpen = isOpen;
        }
    }
}
