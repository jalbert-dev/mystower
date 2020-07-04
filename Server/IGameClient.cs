using System.Linq;
using Server.Data;

namespace Server
{
    public interface IGameMessage
    {
        void Dispatch(IGameClient c);
    }

    public interface IClientProxy
    {
        void EmitMessage(IGameMessage message);
    }

    public interface IGameClient
    {
        /// <summary>
        /// Invoked when an entity that should be known to the client appears,
        /// whether due to spawning or for some other reason.
        /// </summary>
        void HandleMessage(Message.EntityAppeared msg);
        /// <summary>
        /// Invoked when an entity known to the client disappears,
        /// whether due to death or for some other reason.
        /// </summary>
        void HandleMessage(Message.EntityVanished msg);
        /// <summary>
        /// Invoked when an entity known to the client moves from one tile
        /// to another.
        /// </summary>
        void HandleMessage(Message.EntityMoved msg);
        /// <summary>
        /// Invoked when an entity known to the client changes its facing direction.
        /// </summary>
        void HandleMessage(Message.EntityFaced msg);
        
        /// <summary>
        /// Invoked when the server sends new map data to the client.
        /// </summary>
        void HandleMessage(Message.MapChanged msg);

        /// <summary>
        /// Invoked when the server wants the client to display a message by the
        /// given ID in the message log.
        /// </summary>
        void HandleMessage(Message.AddedToLog msg);

        /// <summary>
        /// Invoked when the server wants the client to display the results of
        /// an actor's attack.
        /// </summary>
        void HandleMessage(Message.EntityAttacked msg);
    }
}