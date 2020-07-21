using System;

namespace Util.Functional
{
    [Serializable]
    public class OptionIsNoneException : Exception
    {
        public OptionIsNoneException() { }
        public OptionIsNoneException(string message) : base(message) { }
        public OptionIsNoneException(string message, Exception inner) : base(message, inner) { }
        protected OptionIsNoneException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ResultNotSuccessException : Exception
    {
        public ResultNotSuccessException() { }
        public ResultNotSuccessException(string message) : base(message) { }
        public ResultNotSuccessException(string message, Exception inner) : base(message, inner) { }
        protected ResultNotSuccessException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ResultNotErrorException : Exception
    {
        public ResultNotErrorException() { }
        public ResultNotErrorException(string message) : base(message) { }
        public ResultNotErrorException(string message, Exception inner) : base(message, inner) { }
        protected ResultNotErrorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    // Nullable "T?" don't play nice with generics. Oh well, such is life
    #nullable disable
    public struct Option<T>
    {
        private readonly bool hasValue;
        private readonly T value;

        public bool IsNone => !hasValue;
        public T Value => hasValue ? value : throw new OptionIsNoneException();

        private Option(bool hasValue, T value)
        {
            this.hasValue = hasValue;
            this.value = value;
        }

        public static Option<T> Some(T value) => new Option<T>(true, value);
        public static Option<T> None() => new Option<T>(false, default);

        public static implicit operator Option<T>(Option.GenericNone _) 
            => None();

        public Option<U> Bind<U>(Func<T, Option<U>> f)
            => hasValue ? f(value) : Option.None;
        public Option<U> Map<U>(Func<T, U> f)
            => Bind(x => Option.Some(f(x)));

        public U Match<U>(Func<T, U> some, Func<U> none)
            => hasValue ? some(value) : none();
    }
    
    public interface IError
    {
        string Message { get; }
    }

    public struct Result<TValue>
    {
        private readonly TValue value;
        private readonly IError error;

        public bool IsSuccess { get; }
        public TValue Value => IsSuccess ? value : throw new ResultNotSuccessException();
        public IError Err => !IsSuccess ? error : throw new ResultNotErrorException();

        private Result(TValue resultValue)
        {
            IsSuccess = true;
            value = resultValue;
            error = default;
        }

        private Result(IError err)
        {
            IsSuccess = false;
            value = default;
            error = err;
        }

        public static Result<TValue> Ok(TValue value) => new Result<TValue>(value);
        public static Result<TValue> Error(IError value) => new Result<TValue>(value);

        public static implicit operator Result<TValue>(Result.GenericError err)
            => Error(err.error);

        public Result<U> Bind<U>(Func<TValue, Result<U>> f)
            => IsSuccess ? f(value) : Result.Error(error);
        public Result<U> Map<U>(Func<TValue, U> f)
            => Bind(x => Result.Ok(f(x)));

        public Result<Unit> Finally(Action<TValue> f)
        {
            if (IsSuccess)
            {
                f(value);
                return Result.Ok(Unit.Instance);
            }
            else
            {
                return Result.Error(error);
            }
        }

        public U Match<U>(Func<TValue, U> ok, Func<IError, U> err)
            => IsSuccess ? ok(value) : err(error);
        public void Match(Action<TValue> ok, Action<IError> err)
        {
            if (IsSuccess) 
                ok(value);
            else 
                err(error);
        }

        public Result<U> Select<U>(Func<TValue, U> f) => Map(f);
        public Result<V> SelectMany<U, V>(Func<TValue, Result<U>> f, Func<TValue, U, V> g)
        {
            var self = this;
            return Bind(f).Map(x => g(self.value, x));
        }
        public Result<U> Cast<U>() => Map(x => (U)Convert.ChangeType(x, typeof(U)));
    }
    #nullable enable

    public class Unit
    {
        public static Unit Instance = new Unit();
    }
    
    public static class Option
    {
        public class GenericNone
        {
            public static GenericNone Instance = new GenericNone();
        }

        public static Option<T> Some<T>(T value) 
            => Option<T>.Some(value);
        public static GenericNone None => GenericNone.Instance;
    }

    public static class Result
    {
        public struct GenericError
        {
            public IError error;
        }

        public static Result<Unit> Ok() => Ok(Unit.Instance);
        public static Result<Unit> Ok(Action action)
        {
            action();
            return Ok(Unit.Instance);
        }
        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
        public static GenericError Error(IError error) => new GenericError { error = error };
    }
}