using System;

namespace Server
{
    /// <summary>
    /// An abstract reference to some data on the server.
    /// 
    /// Temporary access to the referenced data can be acquired using
    /// `GameServer.QueryData<T>`.
    /// </summary>
    /// <typeparam name="T">The type of data this handle points at.</typeparam>
    public struct DataHandle<T> where T : class
    {
        // This is a very barebones data handle for the moment; it just
        // stores a raw reference to the data, and performs a reference equality
        // check to see if two handles are "equal", so it only works on the local
        // machine. (Not that network play is planned anyway)

        internal readonly T Resource;
        internal DataHandle(T res) => Resource = res;

        /// <summary>
        /// Queries the given game state for the data referenced by this handle,
        /// and if successful, passes the data to the given action.
        /// Returns whether the query was successful.
        /// </summary>
        internal bool Query(Data.GameState _, Action<T> action)
        {
            action(Resource);
            return true;
        }

        public bool HandleEquals(DataHandle<T> other) 
            => object.ReferenceEquals(Resource, other.Resource);
    }

    internal static class DataHandleExtensions
    {
        internal static DataHandle<T> ToDataHandle<T>(this T self) where T : class
            => new DataHandle<T>(self);
    }
}