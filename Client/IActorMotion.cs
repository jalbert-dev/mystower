using System;

namespace Client
{
    /// <summary>
    /// Describes a motion performed by a MapActor, in conjunction 
    /// with a Choreographer.
    /// </summary>
    public interface IActorMotion
    {
        /// <summary>
        /// The MapActor that this motion acts on.
        /// </summary>
        MapActor MapActor { get; }

        /// <summary>
        /// If true, prevents any other actors' choreographed motions from 
        /// playing until this motion finishes playing.
        /// </summary>
        bool IsGlobalSequential { get; }

        /// <summary>
        /// If true, requires this motion to finish playing before moving on to
        /// the next.
        /// </summary>
        bool IsActorSequential { get; }

        /// <summary>
        /// Specifies whether this motion is finished and can be destroyed.
        /// </summary>
        bool IsFinished { get; }

        /// <summary>
        /// Applies the motion to the assigned MapActor each tick.
        /// For example, a movement motion would change the positional offset, etc.
        /// </summary>
        void Apply(TimeSpan timeElapsed);
    }
}