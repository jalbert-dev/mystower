using System;

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
    }
}