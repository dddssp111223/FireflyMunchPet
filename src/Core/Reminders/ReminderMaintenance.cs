namespace DesktopPet.Core.Reminders;

public static class ReminderMaintenance
{
    public static ReminderDocument DisableExpiredOneShots(
        ReminderDocument document,
        DateTimeOffset now,
        TimeZoneInfo timeZone)
    {
        var changed = false;
        var items = document.Items.Select(item =>
        {
            if (!item.Enabled || item.Mode != ReminderMode.Scheduled ||
                item.Repeat != ReminderRepeat.Once ||
                ReminderScheduleCalculator.NextScheduledDue(item, now, timeZone) is not null)
                return item;

            changed = true;
            return item with { Enabled = false, HasTriggered = true };
        }).ToArray();

        return changed
            ? new ReminderDocument(ReminderDocument.CurrentVersion, items)
            : document;
    }
}
