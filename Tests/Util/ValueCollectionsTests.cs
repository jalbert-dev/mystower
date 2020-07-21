using Xunit;
using FsCheck.Xunit;
using FluentAssertions;
using FsCheck;
using Util;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Tests.Util.ValueCollections
{
    public class ValueListTests
    {
        [Property] public Property TwoValueListsOfValueTypeWithIdenticalContentsAreEqual()
             => Prop.ForAll(
                    Arb.From(
                        from size in Gen.Choose(0, 100)
                        from elements in Gen.ArrayOf(size, Gen.Choose(-1000, 1000))
                        select (elements.ToValueList(), elements.ToValueList())),
                    arrs => {
                        object.ReferenceEquals(arrs.Item1, arrs.Item2).Should().BeFalse();
                        arrs.Item1.Equals(arrs.Item2).Should().BeTrue();
                    });
        [Fact] public void TwoValueListsOfValueTypeWithDifferingContentsAreNotEqual()
        {
            var a = new ValueList<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var b = new ValueList<int> { 1, 2, 3, 4, 5, 6, 7, 8, 8 };

            a.Equals(b).Should().BeFalse();
        }

        // TODO: .Equal() tests for struct and IEquatable class

        struct TestStruct : IEquatable<TestStruct>
        {
            public string str;

            public bool Equals([AllowNull] TestStruct other) => str == other.str;
            public override string ToString() => this.ToPrettyJson();
        }

        class TestCloneable : IEquatable<TestCloneable>, IDeepCloneable<TestCloneable>
        {
            public readonly TestStruct data;
            public TestCloneable(TestStruct data) => this.data = data;
            public TestCloneable DeepClone() => new TestCloneable(data);
            public bool Equals([AllowNull] TestCloneable other)
                => other != null && data.Equals(other.data);
            public override string ToString() => this.ToPrettyJson();
        }

        class TestUncloneable : IEquatable<TestUncloneable>
        {
            public readonly TestStruct data;
            public TestUncloneable(TestStruct data) => this.data = data;
            public bool Equals([AllowNull] TestUncloneable other)
                => other != null && data.Equals(other.data);
            public override string ToString() => this.ToPrettyJson();
        }

        [Property] public Property DeepCloneReturnsNewIdenticalListForValueTypes()
             => Prop.ForAll(
                    Arb.From(
                        from size in Gen.Choose(0, 100)
                        from elements in Gen.ArrayOf(size, Gen.Choose(-1000, 1000))
                        select elements.ToValueList()),
                    list => {
                        var clone = list.DeepClone();
                        object.ReferenceEquals(list, clone).Should().BeFalse();

                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].Should().Be(clone[i]);
                        }
                    });
        [Property] public Property DeepCloneReturnsNewIdenticalListForStructs()
             => Prop.ForAll(
                    Arb.From(
                        from size in Gen.Choose(0, 100)
                        from elements in Gen.ArrayOf(size, Arb.Default.String().Generator)
                        select elements
                            .Select(x => new TestStruct { str = x })
                            .ToValueList()),
                    list => {
                        var clone = list.DeepClone();
                        object.ReferenceEquals(list, clone).Should().BeFalse();

                        for (int i = 0; i < list.Count; i++)
                        {
                            list[i].Should().BeEquivalentTo(clone[i]);
                        }
                    });
        [Property] public Property DeepCloneReturnsNewIdenticalListForDeepCloneable()
             => Prop.ForAll(
                    Arb.From(
                        from size in Gen.Choose(0, 100)
                        from elements in Gen.ArrayOf(size, Arb.Default.String().Generator)
                        select elements
                            .Select(x => new TestCloneable(new TestStruct { str = x }))
                            .ToValueList()),
                    list => {
                        var clone = list.DeepClone();
                        object.ReferenceEquals(list, clone).Should().BeFalse();

                        for (int i = 0; i < list.Count; i++)
                        {
                            object.ReferenceEquals(list[i], clone[i]).Should().BeFalse();
                            list[i].Should().BeEquivalentTo(clone[i]);
                        }
                    });

        [Property] public Property DeepCloneThrowsIfElementTypeNotDeepCloneable()
             => Prop.ForAll(
                    Arb.From(
                        from size in Gen.Choose(0, 100)
                        from elements in Gen.ArrayOf(size, Arb.Default.String().Generator)
                        select elements
                            .Select(x => new TestUncloneable(new TestStruct { str = x }))
                            .ToValueList()),
                    list => {
                        FluentActions.Invoking(() => list.DeepClone()).Should().Throw<Exception>();
                    });
    }    
}