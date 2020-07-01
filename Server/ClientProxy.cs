using System.Collections.Generic;
using System.Linq;

namespace Server
{
    public class ClientProxy : IClientProxy
    {
        List<IGameMessage> messages = new List<IGameMessage>();

        public void EmitMessage(IGameMessage message) => messages.Add(message);
        public List<IGameMessage> PopMessages()
        {
            var messagesCopy = messages.ToList();
            messages.Clear();
            return messagesCopy;
        }
    }
}