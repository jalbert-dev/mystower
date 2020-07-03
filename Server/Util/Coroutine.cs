using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Util
{
    public static class Coroutines
    {
        /// <summary>
        /// Creates an enumerable that waits for the given number of updates
        /// before returning.
        /// </summary>
        public static IEnumerable WaitForFrames(int frameCount)
        {
            for (; frameCount >= 0; frameCount--)
                yield return null;
        }

        /// <summary>
        /// Creates an enumerable from an asynchronous Task. When the Task
        /// completes, the enumerable will return.
        /// </summary>
        /// <param name="task">A task to watch.</param>
        public static IEnumerable WaitForTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;
        }
    }

    /// <summary>
    /// A Coroutine object represents a stack of enumerables, where only the 
    /// topmost enumerable may be executed, and must complete to be removed
    /// from the stack. Yielding another enumerable from inside the topmost
    /// enumerable will push it to this stack.
    /// 
    /// Coroutines are deemed 'done' when the callstack is empty.
    /// </summary>
    public class Coroutine
    {
        private const int INITIAL_STACK_SIZE = 8;

        public Coroutine(IEnumerable enumerable)
        {
            callstack = new Stack<IEnumerator>(INITIAL_STACK_SIZE);
            Add(enumerable);
        }

        bool InnerStep()
        {
            if (IsDone)
                return false;
            
            var co = callstack.Peek();
            while (co.MoveNext() == false)
            {
                callstack.Pop();

                if (IsDone)
                    return false;
                
                co = callstack.Peek();
            }
            
            if (co.Current is IEnumerable nestedCo)
            {
                Add(nestedCo);
                return true;
            }
            else if (co.Current is Task task)
            {
                Add(Coroutines.WaitForTask(task));
                return true;
            }
            return false;
        }

        public void Step()
        {
            while (InnerStep());
        }

        public bool IsDone => callstack.Count == 0;

        void Add(IEnumerable enumerable) => callstack.Push(enumerable.GetEnumerator());

        Stack<IEnumerator> callstack;
    }

    public class CoroutineContainer
    {
        List<Coroutine> coroutines = new List<Coroutine>();

        /// <summary>
        /// Creates a coroutine for the given enumerable and adds it to the
        /// CoroutineContainer. This method does not begin execution of the
        /// created coroutine.
        /// </summary>
        public void Add(IEnumerable co)
        {
            var coroutine = new Coroutine(co);
            coroutines.Add(coroutine);
        }
        /// <summary>
        /// Creates a coroutine, adds it to the CoroutineContainer, and
        /// immediately executes it once.
        /// </summary>
        public void AddAndExecute(IEnumerable co) 
        {
            Add(co);
            coroutines.Last().Step();
        }
        /// <summary>
        /// Removes all coroutines from the CoroutineContainer.
        /// </summary>
        public void ClearAll() => coroutines.Clear();

        /// <summary>
        /// Executes every coroutine in the CoroutineContainer once,
        /// after which any finished coroutine is removed from the container.
        /// </summary>
        public void Update()
        {
            coroutines.RemoveAll(coroutine => {
                coroutine.Step();
                return coroutine.IsDone;
            });
        }
    }
}