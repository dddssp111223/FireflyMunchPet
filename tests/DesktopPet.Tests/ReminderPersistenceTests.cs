using DesktopPet.Core.Reminders;

namespace DesktopPet.Tests;

internal static class ReminderPersistenceTests
{
    public static void Run()
    {
        var document = ReminderDocument.CreateDefault();
        var json = ReminderJson.Serialize(document);
        var roundTrip = ReminderJson.Deserialize(json);
        AssertEx.Equal(document.Version, roundTrip.Version, "reminder json version");
        AssertEx.Equal(document.Items[0], roundTrip.Items[0], "reminder json item round trip");

        var empty = ReminderJson.Deserialize("""{"version":1,"items":[]}""");
        AssertEx.Equal(0, empty.Items.Count, "deleted default does not reappear");

        AssertEx.Throws<InvalidDataException>(
            () => ReminderJson.Deserialize("{broken"),
            "corrupt reminder json rejected");

        var duplicate = document.Items[0];
        AssertEx.Throws<InvalidDataException>(
            () => ReminderJson.Deserialize(ReminderJson.Serialize(
                new ReminderDocument(1, new[] { duplicate, duplicate }))),
            "duplicate ids rejected");

        var directory = Path.Combine(
            Path.GetTempPath(),
            "MunchPetReminderTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "reminders.json");
            var repository = new ReminderRepository(
                path,
                () => new DateTimeOffset(2026, 8, 3, 9, 10, 11, TimeSpan.Zero));

            var created = repository.LoadOrCreate();
            AssertEx.Equal(1, created.Items.Count, "missing file creates default");
            AssertEx.Equal(document.Items[0].Text, created.Items[0].Text, "default text is stable");

            repository.Save(created.Remove(created.Items[0].Id));
            AssertEx.Equal(0, repository.LoadOrCreate().Items.Count, "empty user list persists");

            File.WriteAllText(path, "{broken");
            AssertEx.Equal(1, repository.LoadOrCreate().Items.Count, "corrupt file falls back to default");
            AssertEx.Equal(
                1,
                Directory.GetFiles(directory, "reminders.broken-*.json").Length,
                "corrupt backup retained");
            AssertEx.Equal(false, File.Exists(path + ".tmp"), "temporary save file removed");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
