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
        /// This affects all effects, regardless of what actor they are attached to.
        /// </summary>
        bool IsGlobalSolo { get; }

        /// <summary>
        /// Specifies whether this effect allows other effects to play simultaneously.
        /// This affects only other effects attached to this actor.
        /// </summary>
        bool IsLocalSolo { get; }

        /// <summary>
        /// Specifies whether this effect is finished and can be destroyed.
        /// </summary>
        bool IsDone { get; }

        void Apply(TimeSpan timeElapsed);
    }
}