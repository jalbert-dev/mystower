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
                        from rngSeed in Arb.Default.DoNotSizeUInt64().Generator
                        from map in TileMapGen.Default().Generator
                        from actorCount in Gen.Choose(0, 10)
                        from actors in ActorGen.Default().WithPositionOnMap(map).Generator.ArrayOf(actorCount)
                        select new GameState(actors.ToValueList(), map, new global::Server.Random.LCG64RandomSource(rngSeed.Item))),
                    state => {
                        var sw = new StringWriter();
                        var db = ActorGen.DefaultDatabase;
                        GameStateIO.SaveToStream(state, sw, db).Should().BeNull(
                            "because saving game state should not result in an error");
                        var srcSerialized = sw.ToString();
                        var loadedState = GameStateIO.LoadFromString(srcSerialized, db);

                        loadedState.IsSuccess.Should().BeTrue();
                        loadedState.Value.Should().BeEquivalentTo(state,
                            "because game state after loading should be identical to original state");
                    });
    }
}