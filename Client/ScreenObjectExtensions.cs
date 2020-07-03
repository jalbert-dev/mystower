using System;
using System.Collections.Generic;
using SadConsole;

namespace Client
{
    public static class ScreenObjectExtensions
    {
        public static void TraverseChildren<T>(this ScreenObject self, Action<T> action)
        {
            var toVisit = new Queue<IScreenObject>();

            toVisit.Enqueue(self);

            while (toVisit.Count > 0)
            {
                var node = toVisit.Dequeue();
                if (node is T target)
                    action(target);
                
                foreach (var c in node.Children)
                    toVisit.Enqueue(c);
            }
        }
    }
}