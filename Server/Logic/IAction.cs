using System.Collections.Generic;

namespace Server.Logic
{
    public interface IAction
    {
        int Execute(IEnumerable<IGameClient> clients, Data.GameState gs, Data.Actor actor);
    }
}