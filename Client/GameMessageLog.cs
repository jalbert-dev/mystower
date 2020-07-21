using System.Collections.Generic;
using System.Linq;

namespace Client
{
    public interface ILogMessageSource
    {
        delegate void LogMessageHandler(ILogMessageSource msgSource);
        event LogMessageHandler OnNewMessage;
        IEnumerable<string> GetRecentMessages(int count);
    }

    public class GameMessageLog : ILogMessageSource
    {

        private List<string> messages = new List<string>(256);

        public event ILogMessageSource.LogMessageHandler? OnNewMessage;

        // notice that this is oldest message first!
        public IEnumerable<string> GetRecentMessages(int num)
            => AllMessages.Take(num).Reverse();

        public IEnumerable<string> AllMessages
            => messages.Reverse<string>();

        public void AddMessage(string msg) 
        {
            messages.Add(msg);
            OnNewMessage?.Invoke(this);
        }

        public void ClearMessages()
        {
            messages.Clear();
        }
    }
}