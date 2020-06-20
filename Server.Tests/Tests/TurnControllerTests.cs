using System;
using FsCheck;
using FsCheck.Xunit;
using Server.Data;
using Server.Logic;
using Server.Tests.Generators;

namespace Server.Tests.TurnControllerTests
{
    // For a randomly-sized list of actors with random TimeToAct,
    //     * AdvanceTime for any dt must not leave any actor with TimeToAct < 0 (property)
    //                   with dt 0 must leave before and after identical
    //                   (also a fixed-test covering dt > TimeToAct and dt < cases)
    //     * GetNextToAct: use fixed tests; property-based doesn't gain anything here
    //                     make sure to note ANY unit with lowest CT is valid
    //                     test case where everyone is 0; any non-None option is valid
    
    public class AdvanceTime_Actor
    {
        [Property] public Property Dt0MustNotAlterActorState()
            => Prop.ForAll(ActorGen.Default(),
                (actor) => 
                    actor.IsNotMutatedBy(x => TurnController.AdvanceTime(x, 0)));

        [Property] public Property TimeToActIsAlwaysNonNegativeAfter()
            => Prop.ForAll(ActorGen.Default(), 
                (actor) =>
                    Prop.ForAll(Arb.From(Gen.Choose(actor.timeUntilAct - 20, actor.timeUntilAct + 20)),
                    (dt) => {
                        TurnController.AdvanceTime(actor, dt);
                        return actor.timeUntilAct >= 0;
                    })
                );
    }
}