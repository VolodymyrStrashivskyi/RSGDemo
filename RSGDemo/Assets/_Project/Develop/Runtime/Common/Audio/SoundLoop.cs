using UnityEngine;

namespace _Project.Develop.Runtime.Common.Audio
{
    public class SoundLoop
    {
        private const float SILENT = 0.0001f;

        private readonly AudioSource _source;
        private readonly AudioLowPassFilter _filter;
        private readonly float _baseVolume;
        private readonly float _basePitch;

        private bool _isPaused;

        public SoundLoop(AudioSource source, AudioLowPassFilter filter, float baseVolume, float basePitch)
        {
            _source = source;
            _filter = filter;
            _baseVolume = baseVolume;
            _basePitch = basePitch;
            _source.volume = 0f;
        }

        public float Volume { get; private set; }

        public void SetVolume(float share)
        {
            if (_source == null)
                return;

            Volume = share;
            _source.volume = _baseVolume * share;
            bool isAudible = share > SILENT;

            if (isAudible && _source.isPlaying == false)
                Resume();
            else if (isAudible == false && _source.isPlaying)
                Pause();
        }

        public void SetPitch(float share)
        {
            if (_source != null)
                _source.pitch = _basePitch * share;
        }

        public void SetPosition(Vector3 position)
        {
            if (_source != null)
                _source.transform.position = position;
        }

        public void SetCutoff(float frequency)
        {
            if (_filter != null)
                _filter.cutoffFrequency = frequency;
        }

        public void Restart()
        {
            if (_source == null)
                return;

            _source.Stop();
            _source.time = 0f;
            _isPaused = false;
        }

        public void StartAtRandomTime()
        {
            if (_source != null && _source.clip != null)
                _source.time = Random.Range(0f, _source.clip.length);
        }

        public void Release()
        {
            if (_source != null)
                Object.Destroy(_source.gameObject);
        }

        private void Resume()
        {
            if (_isPaused)
                _source.UnPause();
            else
                _source.Play();

            _isPaused = false;
        }

        private void Pause()
        {
            _source.Pause();
            _isPaused = true;
        }
    }
}
