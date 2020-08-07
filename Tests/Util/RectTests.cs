using System;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Newtonsoft.Json;
using Util;
using Xunit;

namespace Tests.Util
{
    public class RectTests
    {
        public static Gen<Rect> GenRect()
             => from x in Gen.Choose(-100000, 100000)
                from y in Gen.Choose(-100000, 100000)
                from w in Gen.Choose(0, 200000)
                from h in Gen.Choose(0, 200000)
                select Rect.FromSize(x,y,w,h);

        [Property] public Property ConstructionFromSizeReturnsRectWithGivenPosSize()
            => Prop.ForAll(
                Arb.Default.Int32().Generator.Four().ToArbitrary(),
                xywh => 
                {
                    var (x,y,w,h) = xywh;
                    var r = Rect.FromSize(x, y, w, h);
                    r.Left.Should().Be(x);
                    r.Top.Should().Be(y);
                    r.Right.Should().Be(x+w);
                    r.Bottom.Should().Be(y+h);
                    r.Width.Should().Be(w);
                    r.Height.Should().Be(h);
                });

        [Property] public Property ConstructionFromBoundsReturnsRectWithGivenBounds()
            => Prop.ForAll(
                Arb.Default.Int32().Generator.Four().ToArbitrary(),
                ltrb => 
                {
                    var (l,t,r,b) = ltrb;
                    var rect = Rect.FromBounds(l, t, r, b);
                    rect.Left.Should().Be(l);
                    rect.Top.Should().Be(t);
                    rect.Right.Should().Be(r);
                    rect.Bottom.Should().Be(b);
                    rect.Width.Should().Be(r-l);
                    rect.Height.Should().Be(b-t);
                });

        [Property] public Property RectsWithEqualBoundsAreEqual()
             => Prop.ForAll(
                    (from a in GenRect()
                    select (a, a)).ToArbitrary(),
                    pair => {
                        object.ReferenceEquals(pair.Item1, pair.Item2).Should().BeFalse();

                        pair.Item1.Equals(pair.Item2).Should().BeTrue();
                        ((object)pair.Item1).Equals((object)pair.Item2).Should().BeTrue();
                        (pair.Item1 == pair.Item2).Should().BeTrue();
                        (pair.Item1 != pair.Item2).Should().BeFalse();

                        pair.Item1.GetHashCode().Should().Be(pair.Item2.GetHashCode());
                    });

        [Property] public Property RectsWithDifferingBoundsAreNotEqual()
             => Prop.ForAll(
                    (from a in GenRect()
                    from b in GenRect().Where(x => x.Left != a.Left || x.Right != a.Right || x.Top != a.Top || x.Bottom != a.Bottom)
                    select (a, b)).ToArbitrary(),
                    pair => {
                        object.ReferenceEquals(pair.Item1, pair.Item2).Should().BeFalse();

                        pair.Item1.Equals(pair.Item2).Should().BeFalse();
                        ((object)pair.Item1).Equals((object)pair.Item2).Should().BeFalse();
                        (pair.Item1 == pair.Item2).Should().BeFalse();
                        (pair.Item1 != pair.Item2).Should().BeTrue();
                    });
    }
}