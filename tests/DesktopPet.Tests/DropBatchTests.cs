using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class DropBatchTests
{
    public static void Run()
    {
        var batch = DropBatch.Create(new[] { @"C:\a.txt", @"C:\a.txt", @"C:\b" }, 10, 20);

        AssertEx.Equal(2, batch.Paths.Count, "deduplicates paths");
        AssertEx.Equal(10, batch.DropX, "keeps drop x");
        AssertEx.Equal(20, batch.DropY, "keeps drop y");
        AssertEx.Throws<ArgumentException>(
            () => DropBatch.Create(new[] { "", " " }, 0, 0),
            "empty batch is rejected");
    }
}
