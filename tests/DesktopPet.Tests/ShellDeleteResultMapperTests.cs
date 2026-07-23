using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class ShellDeleteResultMapperTests
{
    public static void Run()
    {
        AssertEx.Equal(DeleteOutcome.Succeeded,
            ShellDeleteResultMapper.Map(0, false), "successful Shell operation");
        AssertEx.Equal(DeleteOutcome.Cancelled,
            ShellDeleteResultMapper.Map(0, true), "aborted Shell operation");
        AssertEx.Equal(DeleteOutcome.Failed,
            ShellDeleteResultMapper.Map(5, false), "Shell error");
    }
}
