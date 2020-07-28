using System.Collections.Generic;
using System.Linq;

namespace Server.Random
{
    public interface IRandomSource
    {
        int Next();
        int Next(int min, int max);
    }

    public static class RandomExtensions
    {
        public static T PickFrom<T>(this IRandomSource rng, IEnumerable<T> x) 
            => x.ElementAt(rng.Next(0, x.Count() - 1));
        public static IEnumerable<T> Shuffle<T>(this IRandomSource rng, IEnumerable<T> x)
            => x.OrderBy(_ => rng.Next());
    }
}