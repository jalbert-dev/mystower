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

    public class CoroutineContainer
    {
        private const int INITIAL_STACK_SIZE = 8;

        List<Stack<IEnumerator>> coroutines = new List<Stack<IEnumerator>>();

        public void Start(IEnumerable co) 
        {
            var execStack = new Stack<IEnumerator>(INITIAL_STACK_SIZE);
            execStack.Push(co.GetEnumerator());
            coroutines.Add(execStack);
        }
        public void ClearAll() => coroutines.Clear();

        public void Update()
        {
            coroutines.RemoveAll(coStack => {
                var co = coStack.Peek();
                while (co.MoveNext() == false)
                {
                    coStack.Pop();

                    if (coStack.Count == 0)
                        return true;
                    
                    co = coStack.Peek();
                }
                
                if (co.Current is IEnumerable nestedCo)
                {
                    coStack.Push(nestedCo.GetEnumerator());
                }
                else if (co.Current is Task task)
                {
                    coStack.Push(Coroutines.WaitForTask(task).GetEnumerator());
                }
                return false;
            });
        }
    }
}