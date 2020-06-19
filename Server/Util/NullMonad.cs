using System;

namespace Server.Util
{
    [System.Serializable]
    public class OptionIsNoneException : System.Exception
    {
        public OptionIsNoneException() { }
        public OptionIsNoneException(string message) : base(message) { }
        public OptionIsNoneException(string message, System.Exception inner) : base(message, inner) { }
        protected OptionIsNoneException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [System.Serializable]
    public class ResultNotSuccessException : System.Exception
    {
        public ResultNotSuccessException() { }
        public ResultNotSuccessException(string message) : base(message) { }
        public ResultNotSuccessException(string message, System.Exception inner) : base(message, inner) { }
        protected ResultNotSuccessException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [System.Serializable]
    public class ResultNotErrorException : System.Exception
    {
        public ResultNotErrorException() { }
        public ResultNotErrorException(string message) : base(message) { }
        public ResultNotErrorException(string message, System.Exception inner) : base(message, inner) { }
        protected ResultNotErrorException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    // Nullable "T?" don't play nice with generics. Oh well, such is life
    #nullable disable
    public struct Option<T>
    {
        bool hasValue { get; }
        T value { get; }

        public bool IsNone => !hasValue;
        public T Value => hasValue ? value : throw new OptionIsNoneException();

        private Option(bool hasValue, T value)
        {
            this.hasValue = hasValue;
            this.value = value;
        }

        public static Option<T> Some(T value) => new Option<T>(true, value);
        public static Option<T> None() => new Option<T>(false, default(T));

        public static implicit operator Option<T>(Option.GenericNone _) 
            => None();

        public Option<U> Bind<U>(Func<T, Option<U>> f)
            => hasValue ? f(value) : Option.None;
        public Option<U> Map<U>(Func<T, U> f)
            => Bind(x => Option.Some(f(x)));
    }
    public struct Result<TValue>
    {
        bool success { get; }
        TValue value { get; }
        IError error { get; }

        public bool IsSuccess => success;
        public TValue Value => IsSuccess ? value : throw new ResultNotSuccessException();
        public IError Err => !IsSuccess ? error : throw new ResultNotErrorException();

        private Result(TValue value)
        {
            this.success = true;
            this.value = value;
            this.error = default(IError);
        }

        private Result(IError err)
        {
            this.success = false;
            this.value = default(TValue);
            this.error = err;
        }

        public static Result<TValue> Ok(TValue value) => new Result<TValue>(value);
        public static Result<TValue> Error(IError value) => new Result<TValue>(value);

        public static implicit operator Result<TValue>(Result.GenericError err)
            => Error(err.error);

        public Result<U> Bind<U>(Func<TValue, Result<U>> f)
            => success ? f(value) : Result.Error(error);
        public Result<U> Map<U>(Func<TValue, U> f)
            => Bind(x => Result.Ok(f(x)));

        public Result<Unit> Finally(Action<TValue> f)
        {
            if (success)
            {
                f(value);
                return Result.Ok(Unit.Instance);
            }
            else
            {
                return Result.Error(error);
            }
        }
    }
    #nullable enable

    public interface IError
    {
        string Message { get; }
    }

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
        
        public static Option<T> ToOption<T>(this T? value) where T : class
            => value != null ? Some(value) : None;
    }

    public static class Result
    {
        public struct GenericError
        {
            public IError error;
        }

        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);
        public static GenericError Error(IError error) => new GenericError { error = error };
    }
}