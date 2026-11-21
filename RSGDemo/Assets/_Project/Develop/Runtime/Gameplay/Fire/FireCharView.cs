using _Project.Develop.Runtime.Data.Configs;
using UnityEngine;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public class FireCharView : MonoBehaviour
    {
        private const float CHANGE_THRESHOLD = 0.004f;
        private const float NOT_SHOWN = -1f;

        private static readonly int _charId = Shader.PropertyToID("_Char");
        private static readonly int _heatId = Shader.PropertyToID("_Heat");

        private FireSpot _spot;
        private MeshRenderer[] _renderers;
        private FireVisualConfig _config;
        private MaterialPropertyBlock _block;
        private float _shownChar = NOT_SHOWN;
        private float _shownHeat = NOT_SHOWN;

        public void Bind(FireSpot spot, MeshRenderer[] renderers, FireVisualConfig config)
        {
            _spot = spot;
            _renderers = renderers;
            _config = config;
            _block = new MaterialPropertyBlock();
        }

        private void Update()
        {
            if (_spot == null)
                return;

            float charAmount = FireCharLook.GetShownChar(_spot, _config);
            float heat = _spot.GetGlow(_config.EmberLightShare);

            if (Mathf.Abs(charAmount - _shownChar) < CHANGE_THRESHOLD && Mathf.Abs(heat - _shownHeat) < CHANGE_THRESHOLD)
                return;

            _shownChar = charAmount;
            _shownHeat = heat;

            foreach (MeshRenderer target in _renderers)
            {
                if (target == null)
                    continue;

                target.GetPropertyBlock(_block);
                _block.SetFloat(_charId, charAmount);
                _block.SetFloat(_heatId, heat);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
