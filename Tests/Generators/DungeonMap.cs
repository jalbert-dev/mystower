using FsCheck;

using Server.Data;
using Server.Random;
using Server.Logic.MapGen;

namespace Tests.Server.Generators
{
    public static class DungeonMapGen
    {
        private const int DEFAULT_MAX_TEST_MAP_SIZE = 96;

        public static Arbitrary<Dungeon.Parameters> DefaultGenParams(int maxMapSize = DEFAULT_MAX_TEST_MAP_SIZE)
             => Arb.From(
                 from mapSize in Gen.Choose(Dungeon.Parameters.MIN_MAP_SIZE, maxMapSize).Two()
                 let roomMargin = (2, 2)
                 from roomMinSizeX in Gen.Choose(Dungeon.Parameters.MIN_ROOM_SIZE, mapSize.Item1 - roomMargin.Item1 * 2)
                 from roomMinSizeY in Gen.Choose(Dungeon.Parameters.MIN_ROOM_SIZE, mapSize.Item2 - roomMargin.Item2 * 2)
                 from roomMaxSizeX in Gen.Choose(roomMinSizeX, maxMapSize)
                 from roomMaxSizeY in Gen.Choose(roomMinSizeY, maxMapSize)
                 from roomCountMin in Gen.Choose(1, 4)
                 from roomCountMax in Gen.Choose(roomCountMin, 16)
                 select new Dungeon.Parameters(
                     mapWidth: mapSize.Item1,
                     mapHeight: mapSize.Item2,
                     roomMinWidth: roomMinSizeX,
                     roomMinHeight: roomMinSizeY,
                     roomMaxWidth: roomMaxSizeX,
                     roomMaxHeight: roomMaxSizeY,
                     roomCountMin: roomCountMin,
                     roomCountMax: roomCountMax,
                     mapMarginX: roomMargin.Item1,
                     mapMarginY: roomMargin.Item2));

        public static Arbitrary<Dungeon.Parameters> WithRoomCountMin(this Arbitrary<Dungeon.Parameters> arb, int min)
            => arb.Generator.Select(x => { x.RoomCountMin = min; return x; }).ToArbitrary();
        
        public static Arbitrary<TileMap> Default(int maxMapSize = DEFAULT_MAX_TEST_MAP_SIZE)
             => Arb.From(
                 from genParams in DefaultGenParams(maxMapSize).Generator
                 from rng in Arb.Default.DoNotSizeUInt64().Generator
                 select Dungeon.Generate(genParams, new LCG64RandomSource(rng.Item)).Value);
    }
}