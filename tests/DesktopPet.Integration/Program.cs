using DesktopPet.Core;
using DesktopPet.Windows;

if (!args.Contains("--run-recycle-tests", StringComparer.Ordinal))
{
    Console.WriteLine("SKIPPED: explicit disposable recycle test flag required");
    return 0;
}

var root = Path.GetFullPath(Path.Combine(
    Path.GetTempPath(), "MunchPet-Recycle-Test-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);

var file = Path.GetFullPath(Path.Combine(root, "disposable.txt"));
if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Disposable target escaped its verified temp root.");

File.WriteAllText(file, "Created only for the MunchPet Recycle Bin integration test.");
var batch = DropBatch.Create(new[] { file }, 0, 0);
if (batch.Paths.Any(path =>
        !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
    throw new InvalidOperationException("Drop batch escaped its verified temp root.");

var result = new ShellFileOperation(0).MoveToRecycleBin(batch);
if (result.Outcome != DeleteOutcome.Succeeded)
    throw new InvalidOperationException(
        $"Shell recycle operation failed with {result.Outcome}, code {result.ShellCode}.");
if (File.Exists(file))
    throw new InvalidOperationException("Disposable file still exists after Shell operation.");

Directory.Delete(root, recursive: false);
Console.WriteLine("PASS: internally-created disposable file moved to Recycle Bin");
return 0;
