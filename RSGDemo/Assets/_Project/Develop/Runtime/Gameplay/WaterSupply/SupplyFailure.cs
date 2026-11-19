namespace _Project.Develop.Runtime.Gameplay.WaterSupply
{
    public readonly struct SupplyFailure
    {
        public readonly SupplyFailureKind Kind;
        public readonly string Initiator;
        public readonly float Time;

        public SupplyFailure(SupplyFailureKind kind, string initiator, float time)
        {
            Kind = kind;
            Initiator = initiator;
            Time = time;
        }
    }
}
