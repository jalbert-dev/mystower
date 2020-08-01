using System.Collections.Generic;
using System.Linq;
using Util.Functional;

namespace Server.Random
{
    public interface IRandomSource
    {
        int Next();
        int Next(int min, int max);
    }

    public static class RandomExtensions
    {
        public static Option<T> PickFrom<T>(this IRandomSource rng, IEnumerable<T> x)
        {
            var max = x.Count() - 1;
            
            if (max < 0)
                return Option.None;
            
            return Option.Some(x.ElementAt(rng.Next(0, x.Count() - 1)));
        }
        public static IEnumerable<T> Shuffle<T>(this IRandomSource rng, IEnumerable<T> x)
            => x.OrderBy(_ => rng.Next());
    }
}