using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Util
{
    public static class Coroutines
    {
        public static IEnumerable WaitForFrames(int frameCount)
        {
            for (; frameCount >= 0; frameCount--)
                yield return null;
            yield break;
        }

        public static IEnumerable WaitForTask(Task task)
        {
            while (!task.IsCompleted)
                yield return null;
            yield break;
        }
    }

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

        public void Start(IEnumerable co) 
        {
            Add(co);
            coroutines.Last().Step();
        }
        public void Add(IEnumerable co)
        {
            var coroutine = new Coroutine(co);
            coroutines.Add(coroutine);
        }
        public void ClearAll() => coroutines.Clear();

        public void Update()
        {
            coroutines.RemoveAll(coroutine => {
                coroutine.Step();
                return coroutine.IsDone;
            });
        }
    }
}