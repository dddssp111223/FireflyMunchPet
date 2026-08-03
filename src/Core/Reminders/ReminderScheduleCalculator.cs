namespace DesktopPet.Core.Reminders;

public static class ReminderScheduleCalculator
{
    public static TimeSpan CountdownDuration(ReminderDefinition item) =>
        ReminderDefinition.GetCountdownDuration(item);

    public static DateTimeOffset? NextScheduledDue(
        ReminderDefinition item,
        DateTimeOffset nowUtc,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (item.Mode != ReminderMode.Scheduled ||
            ReminderDefinition.Validate(item) != ReminderValidationError.None)
            return null;

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime;
        var time = item.ScheduledTime!.Value;
        DateTime candidate;

        switch (item.Repeat)
        {
            case ReminderRepeat.Once:
                candidate = item.ScheduledDate!.Value.ToDateTime(time);
                if (candidate <= localNow)
                    return null;
                break;

            case ReminderRepeat.Daily:
                candidate = DateOnly.FromDateTime(localNow).ToDateTime(time);
                if (candidate <= localNow)
                    candidate = candidate.AddDays(1);
                break;

            case ReminderRepeat.Workdays:
                candidate = DateOnly.FromDateTime(localNow).ToDateTime(time);
                if (candidate <= localNow)
                    candidate = candidate.AddDays(1);
                while (candidate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    candidate = candidate.AddDays(1);
                break;

            case ReminderRepeat.Weekly:
                var daysAhead = ((int)item.WeeklyDay!.Value - (int)localNow.DayOfWeek + 7) % 7;
                candidate = DateOnly.FromDateTime(localNow)
                    .AddDays(daysAhead)
                    .ToDateTime(time);
                if (candidate <= localNow)
                    candidate = candidate.AddDays(7);
                break;

            default:
                return null;
        }

        return ToUtc(candidate, timeZone);
    }

    private static DateTimeOffset ToUtc(DateTime local, TimeZoneInfo timeZone)
    {
        local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(local))
            local = local.AddMinutes(1);

        var offset = timeZone.IsAmbiguousTime(local)
            ? timeZone.GetAmbiguousTimeOffsets(local).Max()
            : timeZone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }
}
