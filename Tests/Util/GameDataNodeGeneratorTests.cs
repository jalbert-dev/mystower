using Xunit;
using FluentAssertions;
using System.Diagnostics.CodeAnalysis;

namespace Tests.Util
{
    public partial class GameDataNodeGeneratorTests
    {
        struct DumbStruct : System.IEquatable<DumbStruct>
        {
            public int a;
            public int b;
            public string c;

            public bool Equals([AllowNull] DumbStruct other)
            {
                return a == other.a && b == other.b && c == other.c;
            }
        }

        [CodeGen.GameDataNode]
        partial class TestRoot
        {
            TestB innerB;
            TestC innerC;

            int valField;
            string strField;
        }

        [CodeGen.GameDataNode]
        partial class TestB
        {
            string strField;
            TestC obj;
        }

        [CodeGen.GameDataNode]
        partial class TestC
        {
            int val;
            DumbStruct innerStruct;
        }

        TestRoot obj;
        public GameDataNodeGeneratorTests()
        {
            obj = new TestRoot(
                new TestB(
                    "this is a testB!",
                    new TestC(
                        42,
                        new DumbStruct
                        {
                            a = 4,
                            b = 55,
                            c = "This is a dumb struct.c!"
                        }
                    )
                ),
                new TestC(
                    1002,
                    new DumbStruct
                    {
                        a = 5,
                        b = 22,
                        c = "The plot thinnens..."
                    }
                ),
                200204,
                "Reference type says what"
            );
        }

        [Fact] public void GeneratesValidIDeepCloneableImplementation()
        {
            var c = obj.DeepClone();
            c.Should().BeEquivalentTo(obj);
            obj.InnerB = null;
            obj.StrField = "Spice";
            c.Should().NotBeEquivalentTo(obj);
        }

        [Fact] public void GeneratesValidIEquatableTImplementation()
        {
            var c = obj.DeepClone();
            c.Equals(obj).Should().BeTrue();
            obj.InnerB = null;
            obj.StrField = "Spice";
            c.Equals(obj).Should().BeFalse();
        }
    }
}