using System;
using Newtonsoft.Json;
using FluentAssertions;

namespace Tests
{
    public static class Utility
    {
        /// <summary>
        /// Asserts that the caller object is not changed by some action.
        /// </summary>
        /// <param name="action">An action to execute.</param>
        public static void IsNotMutatedBy<T>(this T self, Action<T> action) where T : global::Util.IDeepCloneable<T>
        {
            var before = self.DeepClone();
            action(before);
            before.Should().BeEquivalentTo(self);
        }
    }
}