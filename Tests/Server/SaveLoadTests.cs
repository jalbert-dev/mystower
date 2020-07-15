using System.IO;
using Server.Data;
using Xunit;
using FluentAssertions;
using Server;
using FsCheck;
using Tests.Server.Generators;
using FsCheck.Xunit;
using Util;

namespace Tests.Server
{
    public class SaveLoadTests
    {
        [Property] public Property SavedThenLoadedStateIsSameAsOriginal()
             => Prop.ForAll(
                    Arb.From(
                        from map in TileMapGen.Default().Generator
                        from actorCount in Gen.Choose(0, 10)
                        from actors in ActorGen.Default().WithPositionOnMap(map).Generator.ArrayOf(actorCount)
                        select new GameState(actors.ToValueList(), map)),
                    state => {
                        // doing a deep comparison of two nested data structures isn't really feasible
                        // here like it is in F# where it's more or less automatic so we'll 
                        // compared serialized states. (this does mean unordered collections,
                        // floating-point nonsense, etc. could cause failures...)
                        // TODO: serialization-based equality is really weak
                        var sw = new StringWriter();
                        GameStateIO.SaveToStream(state, sw);
                        var srcSerialized = sw.ToString();
                        var loadedState = GameStateIO.LoadFromString(srcSerialized);

                        loadedState.Should().BeEquivalentTo(state,
                            "because game state after loading should be identical to original state");
                    });
    }
}