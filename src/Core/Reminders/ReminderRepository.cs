using System.Text;

namespace DesktopPet.Core.Reminders;

public sealed class ReminderRepository
{
    private readonly string _path;
    private readonly Func<DateTimeOffset> _now;

    public ReminderRepository(string path, Func<DateTimeOffset>? now = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("A reminder path is required.", nameof(path));

        _path = Path.GetFullPath(path);
        _now = now ?? (() => DateTimeOffset.Now);
    }

    public ReminderDocument LoadOrCreate()
    {
        if (!File.Exists(_path))
        {
            var created = ReminderDocument.CreateDefault();
            Save(created);
            return created;
        }

        try
        {
            return ReminderJson.Deserialize(File.ReadAllText(_path, Encoding.UTF8));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            BackupCorruptFile();
            var fallback = ReminderDocument.CreateDefault();
            Save(fallback);
            return fallback;
        }
    }

    public void Save(ReminderDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _ = ReminderJson.Deserialize(ReminderJson.Serialize(document));

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var temporaryPath = _path + ".tmp";
        try
        {
            File.WriteAllText(
                temporaryPath,
                ReminderJson.Serialize(document),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private void BackupCorruptFile()
    {
        if (!File.Exists(_path))
            return;

        var directory = Path.GetDirectoryName(_path) ?? string.Empty;
        var stamp = _now().ToString("yyyyMMdd-HHmmss");
        var backupPath = Path.Combine(directory, $"reminders.broken-{stamp}.json");
        if (File.Exists(backupPath))
            backupPath = Path.Combine(directory, $"reminders.broken-{stamp}-{Guid.NewGuid():N}.json");
        File.Move(_path, backupPath);
    }
}
