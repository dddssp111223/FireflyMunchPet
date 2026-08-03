using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class PetStateMachineTests
{
    public static void Run()
    {
        FailedDeleteRejects();
        SuccessfulDeleteSwallowsAndBlocksAnotherFeed();
        CancelledDeleteReturnsToIdle();
        ReminderBounceIsTransientAndBusy();
    }

    private static void FailedDeleteRejects()
    {
        var machine = new PetStateMachine();

        AssertEx.True(machine.EnterFileHover(), "idle -> file hover");
        AssertEx.True(machine.BeginShellPending(), "hover -> pending");
        AssertEx.Equal(PetState.Rejecting, machine.ResolveDelete(DeleteOutcome.Failed), "failed rejects");

        machine.FinishTransient();
        AssertEx.Equal(PetState.Idle, machine.State, "reject -> idle");
    }

    private static void SuccessfulDeleteSwallowsAndBlocksAnotherFeed()
    {
        var machine = new PetStateMachine();
        machine.EnterFileHover();
        machine.BeginShellPending();

        AssertEx.Equal(PetState.Swallowing, machine.ResolveDelete(DeleteOutcome.Succeeded), "success swallows");
        AssertEx.True(!machine.EnterFileHover(), "busy rejects another feed");
    }

    private static void CancelledDeleteReturnsToIdle()
    {
        var machine = new PetStateMachine();
        machine.EnterFileHover();
        machine.BeginShellPending();

        AssertEx.Equal(PetState.Idle, machine.ResolveDelete(DeleteOutcome.Cancelled), "cancel returns idle");
    }

    private static void ReminderBounceIsTransientAndBusy()
    {
        var machine = new PetStateMachine();
        AssertEx.True(machine.BeginReminderBounce(), "reminder bounce begins from idle");
        AssertEx.Equal(PetState.ReminderBounce, machine.State, "reminder bounce state");
        AssertEx.True(machine.IsBusy, "reminder bounce blocks conflicting interaction");
        AssertEx.True(!machine.BeginClickBounce(), "manual click cannot overlap reminder bounce");
        machine.FinishTransient();
        AssertEx.Equal(PetState.Idle, machine.State, "reminder bounce returns idle");
    }
}
