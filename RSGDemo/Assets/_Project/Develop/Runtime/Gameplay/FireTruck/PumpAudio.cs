using _Project.Develop.Runtime.Common.Audio;
using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using _Project.Develop.Runtime.Utils;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.FireTruck
{
    public class PumpAudio : MonoBehaviour
    {
        [SerializeField] private Transform _pumpPoint;
        [SerializeField] private PumpAudioConfig _config;

        private IWaterSupplyChain _waterSupplyChain;
        private SoundLoop _engine;
        private float _load;

        [Inject]
        public void Construct(IWaterSupplyChain waterSupplyChain, IAudioService audioService)
        {
            _waterSupplyChain = waterSupplyChain;
            _engine = audioService.StartLoop(_config.Engine);
            _engine.StartAtRandomTime();
        }

        private void Update()
        {
            if (_engine == null)
                return;

            float targetLoad = Mathf.Clamp01(_waterSupplyChain.DeliveredFlow / _config.FullFlow);
            _load = Mathf.Lerp(_load, targetLoad, Smoothing.GetBlend(_config.Sharpness, Time.deltaTime));

            _engine.SetPosition(_pumpPoint.position);
            _engine.SetPitch(Mathf.Lerp(_config.PitchByLoad.x, _config.PitchByLoad.y, _load));
            _engine.SetVolume(Mathf.Lerp(_config.IdleVolume, 1f, _load));
        }

        private void OnDestroy() =>
            _engine?.Release();
    }
}
