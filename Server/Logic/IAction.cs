using System.Collections.Generic;

namespace Server.Logic
{
    public interface IAction
    {
        void Execute(IEnumerable<IGameClient> clients, Data.GameState gs, Data.Actor actor);
    }
}