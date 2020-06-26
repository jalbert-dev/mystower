using System;

namespace Client
{
    public interface IChoreography
    {
        /// <summary>
        /// The MapActor that this effect acts on.
        /// </summary>
        MapActor MapActor { get; }

        /// <summary>
        /// Specifies whether this effect allows other effects to play simultaneously.
        /// </summary>
        bool IsSolo { get; }

        /// <summary>
        /// Specifies whether this effect is finished and can be destroyed.
        /// </summary>
        bool IsDone { get; }

        void Apply(TimeSpan timeElapsed);
    }
}