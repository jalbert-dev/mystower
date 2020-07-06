using System;
using Newtonsoft.Json;

namespace Tests
{
    public static class Utility
    {
        /// <summary>
        /// Returns whether the caller object is different after some action is
        /// performed on it.
        /// </summary>
        /// <param name="action">An action to execute.</param>
        /// <returns>Whether the object is different after executing the action.</returns>
        public static bool IsNotMutatedBy<T>(this T self, Action<T> action)
        {
            // TODO: This sucks too and is a stopgap measure until I write some
            //       deep clone operation
            var before = JsonConvert.SerializeObject(self);
            action(self);
            return before == JsonConvert.SerializeObject(self);
        }
    }
}