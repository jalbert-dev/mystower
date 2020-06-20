using System;
using Newtonsoft.Json;

namespace Server.Tests
{
    public static class Utility
    {
        // TODO: this is a weak serialization-based structural equality tester. 
        //       need to actually write or find a reflection-based solution...
        public static bool DeepEquals<T>(this T a, T b)
        {
            return JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b);
        }

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