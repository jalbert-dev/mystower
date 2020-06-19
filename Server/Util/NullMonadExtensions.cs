using System;

namespace Server.Util
{
    public static class NullMonadExtensions
    {
        /// <summary>
        /// Converts an Option to a Result, mapping None to the result of a given error thunk.
        /// </summary>
        /// <param name="thunk">A thunk to be called if the Option is None that returns an IError instance.</param>
        public static Result<T> ErrorIfNone<T>(this Option<T> self, Func<IError> thunk)
            => self.IsNone ?
                Result.Error(thunk()) :
                Result.Ok(self.Value);
    }
}