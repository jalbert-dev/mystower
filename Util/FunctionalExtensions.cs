using System;
using System.Collections.Generic;
using System.Linq;

namespace Util.Functional
{
    public static class FunctionalExtensions
    {
        /// <summary>
        /// Converts an Option to a Result, mapping None to the result of a given error thunk.
        /// </summary>
        /// <param name="thunk">A thunk to be called if the Option is None that returns an IError instance.</param>
        public static Result<T> ErrorIfNone<T>(this Option<T> self, Func<IError> thunk)
            => self.IsNone ?
                Result.Error(thunk()) :
                Result.Ok(self.Value);
        
        /// <summary>
        /// Converts a nullable reference type to an Option.
        /// 
        /// Returns Option.None if input is null, else a Some containing the input value.
        /// </summary>
        public static Option<T> ToOption<T>(this T? value) where T : class
            => value != null ? Option.Some(value) : Option.None;

        public static Option<TValue> TryGetValue<TValue, TKey>(this IDictionary<TKey, TValue> self, TKey key)
            => self.TryGetValue(key, out var value) ? Option.Some(value) : Option.None;
        
        public static Result<T> ErrorIfNull<T>(this T? item, Func<IError> ifNull) where T : class
            => item == null ? Result.Error(ifNull()) : Result.Ok(item);

        /// <summary>
        /// Folds over an enumerable starting with a certain seed, where each step
        /// in the fold takes the current aggregate and the current enumerable value,
        /// and returns a Result. An error Result will halt the fold operation.
        /// </summary>
        public static Result<TBindType> FoldBind<TFoldType, TBindType>(this IEnumerable<TFoldType> self, TBindType seed, Func<TBindType, TFoldType, Result<TBindType>> f)
             => self.Aggregate<TFoldType, Result<TBindType>>(
                    Result.Ok(seed),
                    (result, fold) => result.Bind(innerValue => f(innerValue, fold)));
    }
}