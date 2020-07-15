using System.Linq;
using System.Collections.Generic;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Server.Data;
using Server.Logic;
using Tests.Server.Generators;
using Xunit;

namespace Tests.Server.TurnControllerTests
{
    public class GetNextToAct
    {
        [Fact] public void IfAllUnitsReadyAnyIsValidResult()
        {
            IEnumerable<Actor> actors = ActorGen.Default().WithTimeUntilAct(0).Generator.Sample(0, 3);
            var next = TurnController.GetNextToAct(actors);
            next.IsNone.Should().BeFalse();
            actors.Should().Contain(next.Value);
        }

        [Fact] public void UnitWithLowestTimeToActIsResult()
        {
            List<Actor> actors =
                new int[] { 30, 20, 50, 33, 110 }
                .SelectMany(x => ActorGen.Default().WithTimeUntilAct(x).Generator.Sample(1, 1))
                .ToList();
            TurnController.GetNextToAct(actors).Value.Should().BeSameAs(actors[1]);
        }

        [Fact] public void IfMultipleUnitsHaveLowestTimeToActAnyIsValid()
        {
            List<Actor> actors =
                new int[] { 22, 9, 44, 23, 101, 9 }
                .SelectMany(x => ActorGen.Default().WithTimeUntilAct(x).Generator.Sample(1, 1))
                .ToList();
            var next = TurnController.GetNextToAct(actors);
            next.Value.Should().Match(
                x => object.ReferenceEquals(x, actors[1]) || object.ReferenceEquals(x, actors[5])
            );
        }

        [Fact] public void EmptyActorListHasNoneNext()
            => TurnController.GetNextToAct(new Actor[]{}).IsNone.Should().BeTrue();
    }
    
    public class AdvanceTime_Actor
    {
        [Property] public Property Dt0MustNotAlterActorState()
            => Prop.ForAll(ActorGen.Default(),
                (actor) => 
                    actor.IsNotMutatedBy(x => TurnController.AdvanceTime(x, 0)));

        [Property] public Property TimeToActIsAlwaysNonNegativeAfter()
            => Prop.ForAll(ActorGen.Default(), 
                (actor) =>
                    Prop.ForAll(Arb.From(Gen.Choose(actor.TimeUntilAct - 20, actor.TimeUntilAct + 20)),
                    (dt) => {
                        TurnController.AdvanceTime(actor, dt);
                        return actor.TimeUntilAct >= 0;
                    })
                );
        
        [Property] public Property AdvancesTimeByTheGivenDt()
            => Prop.ForAll(ActorGen.Default().WithTimeUntilAct(30),
                actor => {
                    TurnController.AdvanceTime(actor, 12);
                    actor.TimeUntilAct.Should().Be(18);
                    TurnController.AdvanceTime(actor, 24);
                    actor.TimeUntilAct.Should().Be(0);
                    TurnController.AdvanceTime(actor, 0);
                    actor.TimeUntilAct.Should().Be(0);
                    TurnController.AdvanceTime(actor, 300);
                    actor.TimeUntilAct.Should().Be(0);
                    TurnController.AdvanceTime(actor, -18);
                    actor.TimeUntilAct.Should().Be(18);
                    TurnController.AdvanceTime(actor, -18);
                    actor.TimeUntilAct.Should().Be(36);
                    return true;
                });
    }
}