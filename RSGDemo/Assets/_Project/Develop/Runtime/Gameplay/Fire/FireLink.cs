namespace _Project.Develop.Runtime.Gameplay.Fire
{
    public readonly struct FireLink
    {
        public readonly FireSpot Source;
        public readonly FireSpot Target;
        public readonly float Closeness;

        public FireLink(FireSpot source, FireSpot target, float closeness)
        {
            Source = source;
            Target = target;
            Closeness = closeness;
        }
    }
}
