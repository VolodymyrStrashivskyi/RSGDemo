using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterStream;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hydrant
{
    public class HydrantOutflow : MonoBehaviour
    {
        [SerializeField] private WaterStreamView _stream;

        private HydrantSource _hydrant;
        private SupplyLine _supplyLine;
        private WaterStreamConfig _config;
        private Sequence _sequence;
        private bool _isFlowing;

        [Inject]
        public void Construct(HydrantSource hydrant, SupplyLine supplyLine, WaterStreamConfig config)
        {
            _hydrant = hydrant;
            _supplyLine = supplyLine;
            _config = config;

            _hydrant.Opened += OnSupplyChanged;
            _hydrant.Closed += OnSupplyChanged;
            _supplyLine.Connected += OnSupplyChanged;
            _supplyLine.Disconnected += OnSupplyChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            _sequence.Stop();

            if (_hydrant == null)
                return;

            _hydrant.Opened -= OnSupplyChanged;
            _hydrant.Closed -= OnSupplyChanged;
            _supplyLine.Connected -= OnSupplyChanged;
            _supplyLine.Disconnected -= OnSupplyChanged;
        }

        private void OnSupplyChanged(string initiator) =>
            Refresh();

        private void Refresh()
        {
            bool shouldFlow = _hydrant.IsOpen && _supplyLine.IsConnected == false;

            if (shouldFlow == _isFlowing)
                return;

            _isFlowing = shouldFlow;

            if (shouldFlow)
                StartFlow();
            else
                StopFlow();
        }

        private void StartFlow()
        {
            _sequence.Stop();
            _sequence = Sequence.Create()
                .ChainDelay(_config.OpenDelay)
                .ChainCallback(_stream, stream => stream.Burst())
                .Chain(AnimateFlow(_stream.Flow, _config.BurstFlow, _config.BurstTween))
                .Chain(AnimateFlow(_config.BurstFlow, 1f, _config.SettleTween));
        }

        private void StopFlow()
        {
            _sequence.Stop();
            _sequence = Sequence.Create(AnimateFlow(_stream.Flow, 0f, _config.StopTween));
        }

        private Tween AnimateFlow(float from, float to, TweenSettings settings) =>
            Tween.Custom(_stream, new TweenSettings<float>(from, to, settings), (stream, flow) => stream.Flow = flow);
    }
}
