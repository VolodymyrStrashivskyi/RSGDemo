using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace _Project.Develop.Runtime.Gameplay.Fire
{
    [BurstCompile]
    public struct FireCharVolumeJob : IJobParallelFor
    {
        private const float BYTE_MAX = 255f;
        private const int HEAT_SHIFT = 8;

        [ReadOnly] public NativeArray<int> CellStarts;
        [ReadOnly] public NativeArray<int> EntrySpots;
        [ReadOnly] public NativeArray<float> EntryWeights;
        [ReadOnly] public NativeArray<float> SpotChars;
        [ReadOnly] public NativeArray<float> SpotHeats;
        [WriteOnly] public NativeArray<ushort> Pixels;

        public void Execute(int cell)
        {
            float charAmount = 0f;
            float heat = 0f;
            int end = CellStarts[cell + 1];

            for (int entry = CellStarts[cell]; entry < end; entry++)
            {
                int spot = EntrySpots[entry];
                float weight = EntryWeights[entry];
                float spotChar = SpotChars[spot] * weight;
                float spotHeat = SpotHeats[spot] * weight * weight;
                charAmount = spotChar > charAmount ? spotChar : charAmount;
                heat = spotHeat > heat ? spotHeat : heat;
            }

            int charByte = (int)(charAmount * BYTE_MAX);
            int heatByte = (int)(heat * BYTE_MAX);
            Pixels[cell] = (ushort)(charByte | (heatByte << HEAT_SHIFT));
        }
    }
}
