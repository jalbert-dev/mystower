using System.Collections.Generic;

namespace Server.Logic
{
    public interface IAction
    {
        int Execute(IClientProxy client, Data.GameState gs, Data.Actor actor);
    }
}