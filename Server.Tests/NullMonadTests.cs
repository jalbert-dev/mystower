using System;
using Server.Util;
using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

using static Server.Tests.Helpers;

// Unfortunately Result conflicts with FsCheck.Result!
using Res = Server.Util.Result;

namespace Server.Tests
{
    class TestErr : IError
    {
        public string value;
        public string Message => value;
    }
    static class Helpers
    {
        public static Res.GenericError err(string msg) => Res.Error(new TestErr { value = msg });
        public static Util.Result<T> err<T>(string msg) => err(msg);
    }

    public class OptionTests
    {
        [Fact] public void OptionIsNoneReflectsContents()
        {
            Option.Some(5).IsNone.Should().BeFalse();
            ((Option<int>)Option.None).IsNone.Should().BeTrue();
        }

        [Fact] public void OptionWithNullValueIsNotNone()
        {
            Option<object> opt = Option.Some<object>(null);
            opt.IsNone.Should().Be(false);
            opt.Value.Should().BeNull();
        }

        [Property] public bool OptionContainsGivenValue(int x)
            => Option.Some(x).Value == x;

        [Fact] public void OptionAccessingNoneValueThrows()
        {
            Func<int> f = () => ((Option<int>)Option.None).Value;
            f.Should().Throw<OptionIsNoneException>("because Option.None may not have a value");
        }

        [Property] public bool OptionMapCallsMapperWithValue(int x, int y)
        {
            bool mapFunctionWasCalled = false;
            Option<int> mapped = Option.Some(x).Map(value => {
                value.Should().Be(x);
                mapFunctionWasCalled = true;
                return x + y;
            });
            mapFunctionWasCalled.Should().BeTrue(
                "because a non-None Option.Map must call the mapping function");
            return mapped.Value == x + y;
        }

        [Fact] public void OptionNoneMapsToNoneAndDoesntCallMapper()
        {
            Option<int> opt = Option.None;
            var mapperCalled = false;
            opt.Map(x => { mapperCalled = true; return x * x; }).IsNone.Should().BeTrue();
            mapperCalled.Should().BeFalse();
        }
        
        [Property] public bool OptionMapToDifferentType(int x)
        {
            Option<int> opt = Option.Some(x);
            Option<string> mapped = opt.Map(x => x.ToString());
            mapped.IsNone.Should().BeFalse();
            return mapped.Value == x.ToString();
        }

        [Property] public bool OptionBindCallsBinderWithValue(int x, int y)
        {
            bool bindFunctionWasCalled = false;
            Option<int> mapped = Option.Some(x).Bind(value => {
                value.Should().Be(x);
                bindFunctionWasCalled = true;
                return Option.Some(x + y);
            });
            bindFunctionWasCalled.Should().BeTrue(
                "because a non-None Option.Bind must call the binding function");
            return mapped.Value == x + y;
        }

        [Fact] public void OptionNoneBindsToNoneAndDoesntCallMapper()
        {
            Option<int> opt = Option.None;
            var binderCalled = false;
            opt.Bind(x => { binderCalled = true; return Option.Some(x * x); }).IsNone.Should().BeTrue();
            binderCalled.Should().BeFalse();
        }

        [Property] public bool OptionBindToNone(int x)
            => Option.Some(x).Bind<int>(_ => Option.None).IsNone == true;

        [Property] public bool OptionMapBindChainingGivesCorrectValues(int x, int y, int z, int a, int b, int c)
            => Option.Some(x)
                .Map(i => i + y)
                .Map(i => i - z)
                .Bind(i => Option.Some(i + a))
                .Map(i => i - b)
                .Bind(i => Option.Some(i * c))
                .Map(i => i.ToString())
                .Value == ((x + y - z + a - b) * c).ToString();

        [Fact] public void OptionNonePropagates()
        {
            Option.Some(5)
                .Map(x => x + 5)
                .Bind<int>(_ => Option.None)
                .Map(x => x * x)
                .Map(x => x - 3)
                .Bind(x => Option.Some(x + 6))
                .IsNone.Should().BeTrue();
        }
    }

    public class ResultTests
    {
        // Testing Res.Error is fairly contrived compared to an actual use
        // case since the compiler can't infer a type for TValue in a lot of cases,
        // so we get a GenericError instead of the intended Result<_>
        [Fact] public void ResultIsSuccessReflectsContents()
        {
            Res.Ok(5).IsSuccess.Should().BeTrue();
            ((Result<int>)err("")).IsSuccess.Should().BeFalse();
        }

        [Property] public bool ResultOkContainsGivenValue(string str)
            => Res.Ok(str).Value == str;

        [Property] public bool ResultErrContainsGivenError(string str)
        {
            Result<int> result = Res.Error(new TestErr { value=str });
            result.Err.Should().BeOfType<TestErr>();
            return result.Err.Message == str;
        }

        [Fact] public void ResultAccessingErrorOfSuccessResultThrows()
        {
            Func<IError> f = () => Res.Ok(5).Err;
            f.Should().Throw<ResultNotErrorException>("because non-Error Results may not have an error");
        }

        [Fact] public void ResultAccessingValueOfErrorResultThrows()
        {
            Func<int> f = () => ((Result<int>)err("honk")).Value;
            f.Should().Throw<ResultNotSuccessException>("because non-Success Results may not have a value");
        }

        [Property] public bool ResultMapCallsMapperWithValue(int x, int y)
        {
            bool mapperCalled = false;
            var result = Res.Ok(x).Map(value => {
                value.Should().Be(x);
                mapperCalled = true;
                return value + y;
            });
            mapperCalled.Should().BeTrue(
                "because Result.Map called on non-Error must call mapping function");
            return result.Value == x + y;
        }

        [Property] public bool ResultBindsCallsBinderWithValue(int x, int y)
        {
            bool mapperCalled = false;
            var result = Res.Ok(x).Bind(value => {
                value.Should().Be(x);
                mapperCalled = true;
                return Res.Ok(value + y);
            });
            mapperCalled.Should().BeTrue(
                "because Result.Bind called on non-Error must call binding function");
            return result.Value == x + y;
        }

        [Property] public bool ResultMapToDifferentType(int x)
            => Res.Ok(x).Map(x => x.ToString()).Value == x.ToString();

        [Fact] public void ResultMapErrorIsSameErrorAndDoesntCallMapper()
        {
            var result = err<int>("An error!");
            bool mapperCalled = false;
            var mapped = result.Map(x => {
                mapperCalled = true;
                return x.ToString();
            });

            mapped.Err.Should().BeSameAs(result.Err);
            mapperCalled.Should().BeFalse(
                "because Results containing Error must propagate the same error through map/bind");
        }

        [Fact] public void ResultBindErrorIsSameErrorAndDoesntCallBinder()
        {
            var result = err<int>("An error!");
            bool mapperCalled = false;
            var mapped = result.Bind(x => {
                mapperCalled = true;
                return Res.Ok(x.ToString());
            });

            mapped.Err.Should().BeSameAs(result.Err);
            mapperCalled.Should().BeFalse(
                "because Results containing Error must propagate the same error through map/bind");
        }
        
        [Fact] public void ResultWithNullValueIsNotError()
        {
            Result<object> result = Res.Ok<object>(null);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact] public void ResultErrorPropagates()
        {
            Res.Ok(5)
                .Map(x => x + 5)
                .Bind(x => Res.Ok(x - 3))
                .Bind<int>(x => err("This is an error!"))
                .Map(x => x * 64)
                .Bind<int>(x => err("This, too is an error..."))
                .Err.Message.Should().Be("This is an error!",
                    "because the first error in a chain of maps/binds should be the end result");
        }

        [Property] public bool ResultMapBindChainingGivesCorrectValues(int x, int y, int z, int a, int b, int c)
            => Res.Ok(x)
                .Map(i => i + y)
                .Map(i => i - z)
                .Bind(i => Res.Ok(i + a))
                .Map(i => i - b)
                .Bind(i => Res.Ok(i * c))
                .Map(i => i.ToString())
                .Value == ((x + y - z + a - b) * c).ToString();

        [Fact] public void ResultOkFinallyInvokesActionAndGivesUnitResult()
        {
            bool invoked = false;
            Res.Ok(5)
                .Finally(value => {
                    invoked = true;
                    value.Should().Be(5);
                });
            invoked.Should().BeTrue();
        }

        [Fact] public void ResultErrorFinallyDoesntInvokeActionAndGivesSameErrorResult()
        {
            bool invoked = false;
            var result = err<int>("This is an error!");
            var finalResult = result.Finally(value => invoked = true);
            invoked.Should().BeFalse();
            finalResult.Err.Should().BeSameAs(result.Err);
        }
    }

    public class MonadicExtensionsTests
    {
        [Property] public bool ErrorIfNoneMapsOptionWithValueToOkResultWithSameValue(string str)
        {
            return Option.Some(str).ErrorIfNone(() => new TestErr{ value="Oh no"}).Value == str;
        }

        [Fact] public void ErrorIfNoneMapsOptionWithNoneToResultOfThunk()
        {
            bool thunkExecuted = false;
            Option<int> opt = Option.None;
            IError error = new TestErr { value="Oh no" };
            opt.ErrorIfNone(() => {
                thunkExecuted = true;
                return error;
            })
            .Err.Should().BeSameAs(error);
            thunkExecuted.Should().BeTrue();
        }
    }
}
