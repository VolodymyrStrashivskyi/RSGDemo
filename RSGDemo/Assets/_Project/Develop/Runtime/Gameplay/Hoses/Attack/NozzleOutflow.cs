using _Project.Develop.Runtime.Data.Configs;
using _Project.Develop.Runtime.Gameplay.WaterStream;
using _Project.Develop.Runtime.Gameplay.WaterSupply;
using UnityEngine;
using VContainer;

namespace _Project.Develop.Runtime.Gameplay.Hoses.Attack
{
    public class NozzleOutflow : MonoBehaviour
    {
        private const float KILOGRAMS_PER_LITER = 1f;

        [SerializeField] private HoseNozzle _nozzle;
        [SerializeField] private WaterStreamView _stream;

        private IWaterSupplyChain _waterSupplyChain;
        private AttackHoseConfig _config;

        [Inject]
        public void Construct(IWaterSupplyChain waterSupplyChain, AttackHoseConfig config)
        {
            _waterSupplyChain = waterSupplyChain;
            _config = config;
            _stream.FullFlowRate = config.NozzleFlowRate;
        }

        private void Update()
        {
            if (_config == null)
                return;

            bool isOpen = _nozzle.IsOpen;
            _waterSupplyChain.SetDemand(isOpen ? _config.NozzleFlowRate * _nozzle.PressureSetting : 0f);

            float targetFlow = isOpen ? GetDeliveredShare() : 0f;
            float sharpness = targetFlow > _stream.Flow ? _config.FlowOpenSharpness : _config.FlowCloseSharpness;
            float blend = 1f - Mathf.Exp(-sharpness * Time.deltaTime);
            _stream.Flow = Mathf.Lerp(_stream.Flow, targetFlow, blend);
            _stream.Follow(_nozzle.Tip, _nozzle.FirstPersonMuzzle);
            _nozzle.SetReaction(GetReactionForce());
        }

        private float GetReactionForce()
        {
            float massFlow = _config.NozzleFlowRate * _stream.Flow * KILOGRAMS_PER_LITER;
            return massFlow * _stream.ExitSpeed * _config.RecoilScale;
        }

        private float GetDeliveredShare() =>
            _config.NozzleFlowRate > 0f ? _waterSupplyChain.DeliveredFlow / _config.NozzleFlowRate : 0f;
    }
}
