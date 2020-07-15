using System;
using System.Collections.Generic;
using System.Linq;

namespace Util
{
    public class ValueList<T> : List<T>, IEquatable<ValueList<T>> where T : IEquatable<T>
    {
        public bool Equals(ValueList<T>? other) => other != null && this.SequenceEqual(other);
    }

    public static class IEnumerableValueCollectionExtensions
    {
        public static ValueList<T> ToValueList<T>(this IEnumerable<T> self) where T : IEquatable<T>
        {
            var result = new ValueList<T>();
            result.AddRange(self);
            return result;
        }
    }
}