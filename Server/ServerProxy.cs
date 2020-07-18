using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public class ServerProxy : IServerProxy
    {
        List<IGameMessage> messages = new List<IGameMessage>();

        public void EmitClientMessage(IGameMessage message) => messages.Add(message);
        public List<IGameMessage> PopMessages()
        {
            var messagesCopy = messages.ToList();
            messages.Clear();
            return messagesCopy;
        }

        public ServerProxy(Util.Database database)
        {
            this.Database = database;
        }

        public Util.Database Database { get; }
    }
}