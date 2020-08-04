using System;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Newtonsoft.Json;
using Util;
using Xunit;

namespace Tests.Util
{
    public class IntRangeTests
    {
        public static Gen<IntRange> GenRange()
             => from min in Gen.Choose(-100000, 100000)
                from max in Gen.Choose(min, min + 200000)
                select new IntRange(min, max);

        [Fact] public void CreatingWithMaxLessThanMinThrows()
        {
            FluentActions.Invoking(() => new IntRange(0, -1)).Should().Throw<ArgumentException>();
        }

        [Property] public Property RangesWithEqualMinsAndMaxesAreEqual()
             => Prop.ForAll(
                    (from a in GenRange()
                    let b = new IntRange(a.min, a.max)
                    select (a, b)).ToArbitrary(),
                    pair => {
                        object.ReferenceEquals(pair.a, pair.b).Should().BeFalse();

                        pair.a.Equals(pair.b).Should().BeTrue();
                        ((object)pair.a).Equals((object)pair.b).Should().BeTrue();
                        (pair.a == pair.b).Should().BeTrue();
                        (pair.a != pair.b).Should().BeFalse();

                        pair.a.GetHashCode().Should().Be(pair.b.GetHashCode());
                    });

        [Property] public Property RangesWithDifferingMinsOrMaxesAreNotEqual()
             => Prop.ForAll(
                    (from a in GenRange()
                    from b in GenRange().Where(x => x.min != a.min || x.max != a.max)
                    select (a, b)).ToArbitrary(),
                    pair => {
                        object.ReferenceEquals(pair.a, pair.b).Should().BeFalse();

                        pair.a.Equals(pair.b).Should().BeFalse();
                        ((object)pair.a).Equals((object)pair.b).Should().BeFalse();
                        (pair.a == pair.b).Should().BeFalse();
                        (pair.a != pair.b).Should().BeTrue();
                    });

        [Property] public Property RangesAreConvertibleFromIntTuple()
             => Prop.ForAll(
                    GenRange().ToArbitrary(),
                    range => {
                        IntRange r = (range.min, range.max);
                        r.min.Should().Be(range.min);
                        r.max.Should().Be(range.max);
                    });

        [Property] public Property RangesAreDeconstructibleToIntTuple()
             => Prop.ForAll(
                    GenRange().ToArbitrary(),
                    range => {
                        var (min, max) = range;
                        min.Should().BeOfType(typeof(int));
                        max.Should().BeOfType(typeof(int));
                        min.Should().Be(range.min);
                        max.Should().Be(range.max);
                    });
    }

    public class IntRangeConverterTests
    {
        private string ToJson(IntRange x) => JsonConvert.SerializeObject(x, new IntRangeConverter());
        private IntRange FromJson(string x) => JsonConvert.DeserializeObject<IntRange>(x, new IntRangeConverter());

        [Fact] public void ConvertingToJsonRepresentationIsTwoElementArray()
        {
            IntRange x = new IntRange(0, 10);
            x.ToJson().Should().Be("[0,10]");
        }

        [Property] public Property CanBeConvertedFromTwoElementJsonArray()
             => Prop.ForAll(
                    Arb.From(
                        from min in Gen.Choose(-100000, 100000)
                        from max in Gen.Choose(min, min + 200000)
                        select (min, max, $"[{min}, {max}]")),
                    data => {
                        var (expectedMin, expectedMax, json) = data;
                        var result = FromJson(json);

                        result.min.Should().Be(expectedMin);
                        result.max.Should().Be(expectedMax);
                    });
    }
}