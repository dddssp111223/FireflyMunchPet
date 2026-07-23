namespace DesktopPet.Core;

public static class ShellDeleteResultMapper
{
    public static DeleteOutcome Map(int shellResult, bool aborted) =>
        aborted
            ? DeleteOutcome.Cancelled
            : shellResult == 0
                ? DeleteOutcome.Succeeded
                : DeleteOutcome.Failed;
}
