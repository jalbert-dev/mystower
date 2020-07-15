namespace Util
{
    public interface IState<T>
    {
        IState<T>? OnEnter(T obj);
        IState<T>? Exec(T obj);
        IState<T>? OnExit(T obj);
    }

    public abstract class State<T> : IState<T>
    {
        public virtual IState<T>? OnEnter(T obj) => null;
        public virtual IState<T>? Exec(T obj) => null;
        public virtual IState<T>? OnExit(T obj) => null;
    }

    public class StateMachine<T>
    {
        IState<T> current;
        bool needInit = true;

        public bool IsInState(System.Type type) => current.GetType() == type;
        public bool IsInState<U>() => IsInState(typeof(U));

        public StateMachine(IState<T> startState)
        {
            this.current = startState;
        }

        private static IState<T> ResolveTransition(T host, IState<T>? previous, IState<T>? newState)
        {
            if (previous == null && newState == null)
                throw new System.Exception("Attempted to resolve transition with previous and new states null");

            if (newState == null)
                return previous!;

            var interrupt = previous?.OnExit(host);
            newState = interrupt != null ? interrupt : newState;
            return ResolveTransition(host, newState, newState.OnEnter(host));
        }

        public void Exec(T host)
        {
            if (needInit)
            {
                needInit = false;
                current = ResolveTransition(host, null, current);
            }
            
            current = ResolveTransition(host, current, current?.Exec(host));
        }
    }
}