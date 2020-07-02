using System.Collections;
using System.Collections.Generic;
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

        public void Step()
        {
            var co = callstack.Peek();
            while (co.MoveNext() == false)
            {
                callstack.Pop();

                if (IsDone)
                    return;
                
                co = callstack.Peek();
            }
            
            if (co.Current is IEnumerable nestedCo)
            {
                Add(nestedCo);
            }
            else if (co.Current is Task task)
            {
                Add(Coroutines.WaitForTask(task));
            }
        }

        public bool IsDone => callstack.Count == 0;

        void Add(IEnumerable enumerable)
        {
            callstack.Push(enumerable.GetEnumerator());
        }

        Stack<IEnumerator> callstack;
    }

    public class CoroutineContainer
    {

        List<Coroutine> coroutines = new List<Coroutine>();

        public void Start(IEnumerable co) 
        {
            coroutines.Add(new Coroutine(co));
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