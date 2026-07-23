namespace DesktopPet.Core;

public sealed record DropBatch(IReadOnlyList<string> Paths, int DropX, int DropY)
{
    public static DropBatch Create(IEnumerable<string> paths, int x, int y)
    {
        var normalized = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
            throw new ArgumentException("No file-system paths.", nameof(paths));

        return new DropBatch(normalized, x, y);
    }
}
