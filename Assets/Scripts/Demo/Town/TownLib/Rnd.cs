using System;
using System.Diagnostics;

namespace Town
{
    public static class Rnd
    {
        private static int _seed;

        public static int Seed
        {
            get => _seed;
            set
            {
                _seed = value;
                _rndInternal = new Random(Seed);
                Debug.WriteLine($"Random seed {value}");
            }
        }

        private static Random _rndInternal;
        private static Random RndInternal => _rndInternal = _rndInternal ?? new Random(Seed);


        public static int Next(int min, int max)
        {
            return RndInternal.Next(min, max);
        }

        public static int Next(int max)
        {
            return Next(0, max);
        }

        public static double NextDouble()
        {
            return RndInternal.NextDouble();
        }

        public static bool NextBool(float chance)
        {
            return RndInternal.NextDouble() < chance;
        }
    }
}