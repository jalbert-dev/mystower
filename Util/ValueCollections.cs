using System;
using System.Collections.Generic;
using System.Linq;

namespace Util
{
    public class ValueList<T> : List<T>, IEquatable<ValueList<T>>, IDeepCloneable<ValueList<T>> where T : IEquatable<T>
    {
        public ValueList<T> DeepClone()
        {
            if (typeof(T) == typeof(string) || typeof(T).IsValueType)
                return this.ToValueList();
            else if (typeof(T).IsAssignableFrom(typeof(IDeepCloneable<T>)))
                return this.Select(x => ((IDeepCloneable<T>)x).DeepClone()).ToValueList();
            throw new Exception($"Attempted to DeepClone ValueList<T> where T == '{typeof(T).FullName}' (not value-type or IDeepCloneable)");
        }
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