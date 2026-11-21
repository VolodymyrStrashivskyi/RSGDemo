using System.Collections.Generic;

namespace _Project.Develop.Editor.Fire
{
    public class FireSpotLayoutReport
    {
        public int PropsAdded { get; set; }
        public int PropsKept { get; set; }
        public int Storeys { get; set; }
        public int Walls { get; set; }
        public int Facades { get; set; }
        public int Roofs { get; set; }
        public int Total { get; set; }
        public List<string> DuplicateIds { get; set; } = new List<string>();

        public bool HasDuplicates => DuplicateIds.Count > 0;

        public override string ToString()
        {
            string summary = $"Spots in the house: {Total}\n"
                + $"Props: {PropsAdded} new, {PropsKept} already marked\n"
                + $"Storeys found: {Storeys}\n"
                + $"Walls: {Walls}, facades: {Facades}, roof: {Roofs}";

            return HasDuplicates ? $"{summary}\nDuplicate ids: {string.Join(", ", DuplicateIds)}" : summary;
        }
    }
}
