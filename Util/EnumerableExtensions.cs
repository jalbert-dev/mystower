using System.Collections.Generic;

namespace Util
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<T> AsSingleton<T>(this T self) { yield return self; }
    }
}