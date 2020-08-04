using FsCheck;

using Server.Data;
using Server.Random;
using Server.Logic.MapGen;
using System.Reflection;

namespace Tests.Server.Generators
{
    public static class DungeonMapGen
    {
        private const int DEFAULT_MAX_TEST_MAP_SIZE = 96;

        public static Arbitrary<Dungeon.Parameters> DefaultGenParams(int maxMapSize = DEFAULT_MAX_TEST_MAP_SIZE)
             => Arb.From(
                 from mapSize in Gen.Choose(Dungeon.Parameters.MIN_MAP_SIZE, maxMapSize).Two()
                    // .Two() returns Tuple instead of ValueTuple, so...
                    .Select(x => (x.Item1, x.Item2))
                 let mapMargin = (2, 2)
                 from roomMinSizeX in Gen.Choose(Dungeon.Parameters.MIN_ROOM_SIZE, mapSize.Item1 - mapMargin.Item1 * 2)
                 from roomMinSizeY in Gen.Choose(Dungeon.Parameters.MIN_ROOM_SIZE, mapSize.Item2 - mapMargin.Item2 * 2)
                 from roomMaxSizeX in Gen.Choose(roomMinSizeX, maxMapSize)
                 from roomMaxSizeY in Gen.Choose(roomMinSizeY, maxMapSize)
                 from roomCountMin in Gen.Choose(1, 4)
                 from roomCountMax in Gen.Choose(roomCountMin, 16)
                 select new Dungeon.Parameters(
                     mapSize: mapSize,
                     roomWidth: (roomMinSizeX, roomMaxSizeX),
                     roomHeight: (roomMinSizeY, roomMaxSizeY),
                     roomCount: (roomCountMin, roomCountMax),
                     mapMargin: mapMargin));

        public static Arbitrary<Dungeon.Parameters> WithRoomCountMin(this Arbitrary<Dungeon.Parameters> arb, int min)
            => arb.Generator.Select(x => {
                    // TODO: This reflection is pretty ugly. Better to have the DatabaseType/etc generators
                    //       create internal accessors that are exposed to the test assembly, probably.

                    var f = typeof(Dungeon.Parameters).GetField("roomCount", BindingFlags.NonPublic | BindingFlags.Instance)!;

                    object? roomCountObj = f.GetValue(x);

                    typeof(global::Util.IntRange)
                        .GetField("min")!
                        .SetValue(roomCountObj, min);

                    f.SetValue(x, roomCountObj);
                    
                    return x; })
                .ToArbitrary();
        
        public static Arbitrary<TileMap> Default(int maxMapSize = DEFAULT_MAX_TEST_MAP_SIZE)
             => Arb.From(
                 from genParams in DefaultGenParams(maxMapSize).Generator
                 from rng in Arb.Default.DoNotSizeUInt64().Generator
                 select Dungeon.Generate(genParams, new LCG64RandomSource(rng.Item)).Value);
    }
}