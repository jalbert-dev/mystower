using System.IO;
using Server.Data;
using Xunit;
using FluentAssertions;
using Newtonsoft.Json;

namespace Server.Tests
{
    public class SaveLoadTests
    {
        // TODO: This test is bad right now because state isn't randomized.
        //       A helper to generate a random game state would work, but be
        //       hard to keep in sync as new fields get added.
        [Fact] public void SavedThenLoadedStateIsSameAsOriginal()
        {
            // doing a deep comparison of two nested data structures isn't really feasible
            // here like it is in F# where it's more or less automatic so we'll 
            // compared serialized states. (this does mean unordered collections,
            // floating-point nonsense, etc. could cause failures...)
            // TODO: serialization-based equality is really weak
            var state = new GameState();
            var sw = new StringWriter();
            GameStateIO.SaveToStream(state, sw);
            var srcSerialized = sw.ToString();
            var loadedState = GameStateIO.LoadFromString(srcSerialized);

            loadedState.DeepEquals(state).Should().BeTrue(
                "because game state after loading should be identical to original state");
        }
    }
}