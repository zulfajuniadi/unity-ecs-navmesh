namespace Town
{
    public class TownOptions
    {
        public bool Overlay { get; set; }
        public bool Walls { get; set; }
        public bool Water { get; set; }
        public int Patches { get; set; }
        public int Seed { get; set; }

        public static TownOptions Default => new TownOptions { Patches = 35 };
    }
}