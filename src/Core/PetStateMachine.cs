namespace DesktopPet.Core;

public sealed class PetStateMachine
{
    public PetState State { get; private set; } = PetState.Idle;

    public bool IsBusy => State is PetState.ShellPending or PetState.Swallowing;

    public bool EnterFileHover() => Transition(PetState.Idle, PetState.FileHover);

    public bool LeaveFileHover() => Transition(PetState.FileHover, PetState.Idle);

    public bool BeginShellPending() => Transition(PetState.FileHover, PetState.ShellPending);

    public bool BeginClickBounce() => Transition(PetState.Idle, PetState.ClickBounce);

    public bool BeginCheekDrag() => Transition(PetState.Idle, PetState.CheekDragging);

    public bool BeginWindowDrag() => Transition(PetState.Idle, PetState.WindowDragging);

    public PetState ResolveDelete(DeleteOutcome outcome)
    {
        if (State != PetState.ShellPending)
            return State;

        State = outcome switch
        {
            DeleteOutcome.Succeeded => PetState.Swallowing,
            DeleteOutcome.Failed => PetState.Rejecting,
            _ => PetState.Idle
        };
        return State;
    }

    public void FinishTransient()
    {
        if (State is PetState.Swallowing or PetState.ClickBounce or
            PetState.CheekDragging or PetState.WindowDragging or PetState.Rejecting)
        {
            State = PetState.Idle;
        }
    }

    private bool Transition(PetState expected, PetState next)
    {
        if (State != expected)
            return false;

        State = next;
        return true;
    }
}
