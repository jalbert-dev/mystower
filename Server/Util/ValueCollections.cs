using System;
using System.Collections.Generic;
using System.Linq;

namespace Util
{
    public class ValueList<T> : List<T>, IEquatable<ValueList<T>> where T : IEquatable<T>
    {
        public bool Equals(ValueList<T>? other) => other != null && this.SequenceEqual(other);
    }
}