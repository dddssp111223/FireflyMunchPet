namespace DesktopPet.Core;

public enum PetState
{
    Idle,
    FileHover,
    ShellPending,
    Swallowing,
    ClickBounce,
    CheekDragging,
    WindowDragging,
    Rejecting
}

public enum DeleteOutcome
{
    Succeeded,
    Cancelled,
    Failed
}
