using System;
using Util.Functional;
using Xunit;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

using static Tests.Util.FunctionalTests.Helpers;

// Unfortunately Result conflicts with FsCheck.Result!
using Res = Util.Functional.Result;

namespace Tests.Util.FunctionalTests
{
    class TestErr : IError
    {
        public string value = "";
        public string Message => value;
    }
    static class Helpers
    {
        public static Res.GenericError err(string msg) => Res.Error(new TestErr { value = msg });
        public static global::Util.Functional.Result<T> err<T>(string msg) => err(msg);
    }

    public class OptionTests
    {
        [Fact] public void IsNoneReflectsContents()
        {
            Option.Some(5).IsNone.Should().BeFalse();
            ((Option<int>)Option.None).IsNone.Should().BeTrue();
        }

        [Fact] public void NullValueIsNotNone()
        {
            Option<object?> opt = Option.Some<object?>(null);
            opt.IsNone.Should().Be(false);
            opt.Value.Should().BeNull();
        }

        [Property] public bool ContainsGivenValue(int x)
            => Option.Some(x).Value == x;

        [Fact] public void AccessingValueOfNoneThrows()
        {
            Func<int> f = () => ((Option<int>)Option.None).Value;
            f.Should().Throw<OptionIsNoneException>("because Option.None may not have a value");
        }

        [Property] public bool MapCallsMapperWithValue(int x, int y)
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

        [Fact] public void NoneMapsToNoneAndDoesntCallMapper()
        {
            Option<int> opt = Option.None;
            var mapperCalled = false;
            opt.Map(x => { mapperCalled = true; return x * x; }).IsNone.Should().BeTrue();
            mapperCalled.Should().BeFalse();
        }
        
        [Property] public bool MapToDifferentType(int x)
        {
            Option<int> opt = Option.Some(x);
            Option<string> mapped = opt.Map(x => x.ToString());
            mapped.IsNone.Should().BeFalse();
            return mapped.Value == x.ToString();
        }

        [Property] public bool BindCallsBinderWithValue(int x, int y)
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

        [Fact] public void NoneBindsToNoneAndDoesntCallMapper()
        {
            Option<int> opt = Option.None;
            var binderCalled = false;
            opt.Bind(x => { binderCalled = true; return Option.Some(x * x); }).IsNone.Should().BeTrue();
            binderCalled.Should().BeFalse();
        }

        [Property] public bool BindToNoneExample(int x)
            => Option.Some(x).Bind<int>(_ => Option.None).IsNone == true;

        [Property] public bool MapBindChainingExample(int x, int y, int z, int a, int b, int c)
            => Option.Some(x)
                .Map(i => i + y)
                .Map(i => i - z)
                .Bind(i => Option.Some(i + a))
                .Map(i => i - b)
                .Bind(i => Option.Some(i * c))
                .Map(i => i.ToString())
                .Value == ((x + y - z + a - b) * c).ToString();

        [Fact] public void NonePropagates()
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
        [Fact] public void IsSuccessReflectsContents()
        {
            Res.Ok(5).IsSuccess.Should().BeTrue();
            ((Result<int>)err("")).IsSuccess.Should().BeFalse();
        }

        [Property] public bool OkContainsGivenValue(string str)
            => Res.Ok(str).Value == str;

        [Property] public bool ErrContainsGivenError(string str)
        {
            Result<int> result = Res.Error(new TestErr { value=str });
            result.Err.Should().BeOfType<TestErr>();
            return result.Err.Message == str;
        }

        [Fact] public void AccessingErrorOfSuccessResultThrows()
        {
            Func<IError> f = () => Res.Ok(5).Err;
            f.Should().Throw<ResultNotErrorException>("because non-Error Results may not have an error");
        }

        [Fact] public void AccessingValueOfErrorResultThrows()
        {
            Func<int> f = () => ((Result<int>)err("honk")).Value;
            f.Should().Throw<ResultNotSuccessException>("because non-Success Results may not have a value");
        }

        [Property] public bool MapCallsMapperWithValue(int x, int y)
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

        [Property] public bool BindsCallsBinderWithValue(int x, int y)
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

        [Property] public bool MapToDifferentTypeExample(int x)
            => Res.Ok(x).Map(x => x.ToString()).Value == x.ToString();

        [Fact] public void ErrorMapsToSameErrorAndDoesntCallMapper()
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

        [Fact] public void ErrorBindsToSameErrorAndDoesntCallBinder()
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
            Result<object?> result = Res.Ok<object?>(null);
            result.IsSuccess.Should().BeTrue();
        }

        [Fact] public void ErrorPropagates()
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

        [Property] public bool MapBindChainingExample(int x, int y, int z, int a, int b, int c)
            => Res.Ok(x)
                .Map(i => i + y)
                .Map(i => i - z)
                .Bind(i => Res.Ok(i + a))
                .Map(i => i - b)
                .Bind(i => Res.Ok(i * c))
                .Map(i => i.ToString())
                .Value == ((x + y - z + a - b) * c).ToString();

        [Fact] public void FinallyWithOkInvokesActionAndGivesUnitResult()
        {
            bool invoked = false;
            Res.Ok(5)
                .Finally(value => {
                    invoked = true;
                    value.Should().Be(5);
                });
            invoked.Should().BeTrue();
        }

        [Fact] public void FinallyWithErrorDoesntInvokeActionAndGivesSameErrorResult()
        {
            bool invoked = false;
            var result = err<int>("This is an error!");
            var finalResult = result.Finally(value => invoked = true);
            invoked.Should().BeFalse();
            finalResult.Err.Should().BeSameAs(result.Err);
        }

        [Fact] public void LinqQueryOnOkResultProducesOk()
             => (from x in Res.Ok(2) select x * 2)
                    .Should().BeEquivalentTo(Res.Ok(4));
                    
        [Fact] public void LinqQueryOnErrorResultProducesError()
             => (from x in err<int>("Error") select x * 2)
                    .IsSuccess.Should().BeFalse();
        
        [Fact] public void LinqCompoundQueryOnOkResultsProducesOk()
             => (from x in Res.Ok(2)
                 from y in Res.Ok(3)
                 from z in Res.Ok(5)
                 from a in Res.Ok(7)
                 select x + y + z + a)
                .Should().BeEquivalentTo(Res.Ok(2 + 3 + 5 + 7));
        
        [Fact] public void LinqCompoundQueryProducesErrorAndDoesntRunMapper()
        { 
            bool mapperWasExecuted = false;
            bool mapper(int a, int b, int c, int d) => mapperWasExecuted = true;

            var result = 
                from x in Res.Ok(2)
                from y in Res.Ok(5)
                from z in err<int>("error")
                from a in Res.Ok(7)
                select mapper(x, y, z, a);

            result.IsSuccess.Should().BeFalse();
            mapperWasExecuted.Should().BeFalse();
        }
    }

    public class FunctionalExtensionsTests
    {
        [Property] public bool MapsOptionWithValueToOkResultWithSameValue(string str)
        {
            return Option.Some(str).ErrorIfNone(() => new TestErr{ value="Oh no"}).Value == str;
        }

        [Fact] public void MapsOptionWithNoneToResultOfThunk()
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
