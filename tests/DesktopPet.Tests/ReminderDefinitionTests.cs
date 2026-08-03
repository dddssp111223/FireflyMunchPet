using DesktopPet.Core.Reminders;

namespace DesktopPet.Tests;

internal static class ReminderDefinitionTests
{
    public static void Run()
    {
        var document = ReminderDocument.CreateDefault();
        AssertEx.Equal(1, document.Items.Count, "one default reminder");

        var item = document.Items[0];
        AssertEx.Equal(
            "流萤提醒亲爱的，记得站起来运动运动，提提肛哦~",
            item.Text,
            "default reminder text");
        AssertEx.Equal(ReminderMode.Countdown, item.Mode, "default reminder mode");
        AssertEx.Equal(ReminderRepeat.Interval, item.Repeat, "default reminder repeat");
        AssertEx.Equal(40, item.CountdownValue, "default countdown value");
        AssertEx.Equal(CountdownUnit.Minutes, item.CountdownUnit, "default countdown unit");
        AssertEx.True(item.Enabled, "default reminder enabled");

        AssertEx.Equal(
            ReminderValidationError.EmptyText,
            ReminderDefinition.Validate(item with { Text = "  " }),
            "blank text rejected");
        AssertEx.Equal(
            ReminderValidationError.TextTooLong,
            ReminderDefinition.Validate(item with { Text = new string('萤', 201) }),
            "201 characters rejected");
        AssertEx.Equal(
            ReminderValidationError.None,
            ReminderDefinition.Validate(
                item with { Text = string.Concat(Enumerable.Repeat("👩‍🚀", 200)) }),
            "200 grapheme clusters accepted");
        AssertEx.Equal(
            string.Concat(Enumerable.Repeat("👩‍🚀", 200)),
            ReminderDefinition.TrimTextElements(
                string.Concat(Enumerable.Repeat("👩‍🚀", 201)),
                ReminderDefinition.MaxTextElements),
            "text limit truncates whole grapheme clusters");
        AssertEx.Equal(
            ReminderValidationError.InvalidCountdown,
            ReminderDefinition.Validate(item with { CountdownValue = 0 }),
            "zero countdown rejected");
        AssertEx.Equal(
            ReminderValidationError.InvalidRepeat,
            ReminderDefinition.Validate(item with { Repeat = ReminderRepeat.Daily }),
            "calendar repeat rejected for countdown");

        var scheduled = item with
        {
            Mode = ReminderMode.Scheduled,
            Repeat = ReminderRepeat.Weekly,
            ScheduledTime = new TimeOnly(9, 30),
            WeeklyDay = DayOfWeek.Monday,
            CountdownValue = 0
        };
        AssertEx.Equal(
            ReminderValidationError.None,
            ReminderDefinition.Validate(scheduled),
            "weekly schedule accepted");

        AssertEx.Throws<InvalidOperationException>(
            () => new ReminderDocument(
                ReminderDocument.CurrentVersion,
                Enumerable.Range(0, 6)
                    .Select(index => item with { Id = Guid.NewGuid(), Order = index })
                    .ToArray()),
            "six reminders rejected");

        var empty = document.Remove(item.Id);
        AssertEx.Equal(0, empty.Items.Count, "default reminder can be deleted");
        var restored = empty.Upsert(item with { Text = "喝水" });
        AssertEx.Equal("喝水", restored.Items[0].Text, "reminder can be added again");

        var sparse = new ReminderDocument(
            ReminderDocument.CurrentVersion,
            new[] { item with { Order = 0 }, item with { Id = Guid.NewGuid(), Order = 2 } });
        AssertEx.Equal(3, sparse.NextOrder(), "new reminder order follows highest surviving order");

        var now = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        var expired = item with
        {
            Mode = ReminderMode.Scheduled,
            Repeat = ReminderRepeat.Once,
            ScheduledDate = new DateOnly(2026, 8, 2),
            ScheduledTime = new TimeOnly(9, 0),
            CountdownValue = 0
        };
        var normalized = ReminderMaintenance.DisableExpiredOneShots(
            new ReminderDocument(ReminderDocument.CurrentVersion, new[] { expired }),
            now,
            TimeZoneInfo.Utc);
        AssertEx.True(!normalized.Items[0].Enabled, "past one-shot is disabled on load");
        AssertEx.True(normalized.Items[0].HasTriggered, "past one-shot is marked handled");
    }
}
