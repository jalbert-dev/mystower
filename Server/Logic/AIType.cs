using System.Linq;
using Server.Data;
using Server.Logic;
using Util.Functional;

namespace Server.Logic
{
    public static partial class AIType
    {
        /// AI consists of a stateless function that takes a game state and entity,
        /// and returns an optional action to take. (If no action taken, delegates to client.)
        public delegate IAction? ActionSelector(GameState gs, Actor actor);

        // for now, we'll just look up actor AI by name using reflection
        // (this is why this class is partial)
        public static Option<ActionSelector> Lookup(string id)
            => (typeof(AIType)
                .GetProperties()
                .Where(x => x.Name == id)
                .SingleOrDefault()
                ?.GetValue(null)
                as ActionSelector)
                .ToOption();
    }
}