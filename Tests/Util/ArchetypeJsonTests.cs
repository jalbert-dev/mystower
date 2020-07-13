using System.Collections.Generic;
using FluentAssertions;
using Util;
using Xunit;

using U = Util;

namespace Tests.Util
{
    public class ArchetypeJsonTests
    {
        public class TestStruct
        {
            public int a;
            public string b = "";
            public List<int> c = new List<int>();
        }

        [Fact] public void InvalidJsonGivesParseError()
        {
            var result = ArchetypeJson.Read<TestStruct>(@"{ cats are pretty cute! }");

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.JsonParseFailed>();
        }

        [Fact] public void ParsingNormalJsonGivesNormalResults()
        {
            var src = new Dictionary<string, TestStruct>()
            {
                ["item1"] = new TestStruct
                {
                    a = 1,
                    b = "Test!",
                    c = new List<int> { 1, 2, 3, 4 }
                },
                ["item2"] = new TestStruct
                {
                    a = 4,
                    b = "",
                    c = new List<int>()
                },
                ["item3"] = new TestStruct()
            };

            var result = ArchetypeJson.Read<TestStruct>(src.ToJson());

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(src,
                "because the archetype JSON parser should merely be a validation layer");
        }

        [Fact] public void UnspecifiedValuesComeFromSpecifiedArchetype()
        {
            var input = @"{
                ""__archetype"": {
                    ""a"": 42,
                    ""b"": ""From archetype"",
                    ""c"": []
                },
                ""item"": {
                    ""_archetype"": ""__archetype"",

                    ""a"": 3
                }
            }";
            var expected = new TestStruct
            { 
                a = 3,
                b = "From archetype",
                c = new List<int>(),
            };

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeTrue();
            result.Value["item"].Should().BeEquivalentTo(expected);
        }

        [Fact] public void NoArchetypeAndMissingFieldsIsParseFailure()
        {
            var input = @"{
                ""item"": { 
                    ""a"": 3,
                    ""b"": ""a string""
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.FieldNotFound>();
        }

        [Fact] public void SpecifiedButNotFoundArchetypeIsParseFailure()
        {
            var input = @"{
                ""item"": {
                    ""_archetype"": ""this doesn't exist"",

                    ""a"": 4
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.ArchetypeNotFound>();
        }

        [Fact] public void ValueLookupCrawlsArchetypeHierarchy()
        {
            var input = @"{
                ""__a"": {
                    ""a"": 5,
                    ""b"": ""a!"",
                },
                ""__b"": {
                    ""_archetype"": ""__a"",

                    ""c"": [ 1, 2, 3 ]
                },
                ""item"": {
                    ""_archetype"": ""__b"",

                    ""a"": 24
                }
            }";
            var expected = new TestStruct
            {
                a = 24,
                b = "a!",
                c = new List<int> { 1, 2, 3 },
            };

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeTrue();
            result.Value["item"].Should().BeEquivalentTo(expected);
        }

        [Fact] public void ValueLookupCrawlStopsAtFirstArchetypeInHierarchyContainingField()
        {
            var input = @"{
                ""__a"": {
                    ""a"": 1
                },
                ""__b"": {
                    ""_archetype"": ""__a"",

                    ""a"": 2
                },
                ""item"": {
                    ""_archetype"": ""__b"",

                    ""b"": """",
                    ""c"": []
                }
            }";
            var expected = new TestStruct { a = 2 };

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeTrue();
            result.Value["item"].Should().BeEquivalentTo(expected);
        }
        
        [Fact] public void FieldNotInObjectOrArchetypeHierarchyIsParseFailure()
        {
            var input = @"{
                ""__a"": {
                    ""a"": 1
                },
                ""__b"": {
                    ""_archetype"": ""__a"",

                    ""c"": []
                },
                ""item"": {
                    ""_archetype"": ""__b"",
                }
            }";
            
            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.FieldNotFound>();
        }

        [Fact] public void CyclicArchetypeSpecificationIsParseFailure()
        {
            // item -> __b -> __a -> __b -> ...
            var input = @"{
                ""__a"": {
                    ""_archetype"": ""__b""
                },
                ""__b"": {
                    ""_archetype"": ""__a""
                },
                ""item"": {
                    ""_archetype"": ""__b""
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.ArchetypeCycleDetected>();
        }

        [Fact] public void SelfArchetypeIsParseFailure()
        {
            var input = @"{
                ""item"": {
                    ""_archetype"": ""item""
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.ArchetypeCycleDetected>();
        }

        [Fact] public void ArchetypeFieldMustBeString()
        {
            var input = @"{
                ""item"": {
                    ""_archetype"": [1,2,3]
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.ArchetypeFieldNotString>();
        }

        [Fact] public void ArchetypeNodeMustBeObject()
        {
            var input = @"{
                ""__a"": 4,
                ""item"": {
                    ""_archetype"": ""__a""
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);
            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.NodeNotJsonObject>();
        }
        
        [Fact] public void CycleIsParseFailureEvenIfAllFieldsSpecified()
        {
            // item -> __b -> __a -> __b -> ...
            var input = @"{
                ""__a"": {
                    ""_archetype"": ""__b""
                },
                ""__b"": {
                    ""_archetype"": ""__a""
                },
                ""item"": {
                    ""_archetype"": ""__b""

                    ""a"": 3,
                    ""b"": ""text"",
                    ""c"": []
                }
            }";


            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.ArchetypeCycleDetected>();
        }

        [Fact] public void ArchetypeCanBeBelowDescendant()
        {
            var input = @"{
                ""item"": {
                    ""_archetype"": ""__a"",

                    ""a"": 2,
                    ""c"": []
                },
                ""__a"": {
                    ""b"": ""Text!""
                }
            }";
            var expected = new TestStruct
            {
                a = 2,
                b = "Text!",
                c = new List<int>()
            };

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeTrue();
            result.Value["item"].Should().BeEquivalentTo(expected);
        }

        [Fact] public void DoubleUnderscorePrefixedIdIsTemplateAndNotInFinalResult()
        {
            var input = @"{
                ""__test"": {
                    ""a"": 1,
                    ""b"": ""Text!"",
                    ""c"": []
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeTrue();
            result.Value.ContainsKey("__test").Should().BeFalse();
        }

        [Fact] public void LineBeginningWithWhitespaceThenHashIsCommentAndDoesNotAffectParse()
        {
            var input = @"
            # Here's a comment right at the front!
            {
                ""__a"": {
                    # Wow, nobody could've expected to see a comment here...
                    ""a"": 5,
                    ""b"": ""a!"",
                },
                ""__b"": {
                    ""_archetype"": ""__a"",

                    ""c"": [ 1, 2, 3 ]
                },
                   # Did you just put spaces before the comment, you monster
                ""item"": {
                    ""_archetype"": ""__b"",

                    ""a"": 24
                }
# Oh no, my alignment!!!
            }";

            ArchetypeJson.Read<TestStruct>(input).IsSuccess.Should().BeTrue();
        }

        [Fact] public void InlineCommentIsParseFailure()
        {
            var input = @"
            {
                ""a"": { # Nope, inline comments are not allowed!
                    ""a"": 42,
                    ""b"": ""teest"",
                    ""c"": []
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.JsonParseFailed>();
        }

        [Fact] public void NonObjectInRootIsParseFailure()
        {
            var input = @"{
                ""an item"": 3,

                ""item1"": {
                    ""a"": 1,
                    ""b"": ""Test Item 1"",
                    ""c"": [1, 2, 4, 8, 16]
                }
            }";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.NodeNotJsonObject>();
        }

        [Fact] public void RootMustBeObject()
        {
            var input = @"[ 1, 2, 3 ]";

            var result = ArchetypeJson.Read<TestStruct>(input);

            result.IsSuccess.Should().BeFalse();
            result.Err.Should().BeOfType<U.Error.JsonParseFailed>();
        }
    }
}