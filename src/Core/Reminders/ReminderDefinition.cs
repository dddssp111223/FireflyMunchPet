using System.Globalization;

namespace DesktopPet.Core.Reminders;

public enum ReminderMode
{
    Scheduled,
    Countdown
}

public enum ReminderRepeat
{
    Once,
    Daily,
    Workdays,
    Weekly,
    Interval
}

public enum CountdownUnit
{
    Minutes,
    Hours,
    Days
}

public enum ReminderValidationError
{
    None,
    EmptyText,
    TextTooLong,
    InvalidRepeat,
    InvalidSchedule,
    InvalidCountdown
}

public sealed record ReminderDefinition(
    Guid Id,
    string Text,
    bool Enabled,
    int Order,
    ReminderMode Mode,
    ReminderRepeat Repeat,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    DayOfWeek? WeeklyDay,
    int CountdownValue,
    CountdownUnit CountdownUnit,
    bool HasTriggered)
{
    public const int MaxTextElements = 200;

    public static ReminderDefinition CreateDefault() => new(
        Guid.NewGuid(),
        "流萤提醒亲爱的，记得站起来运动运动，提提肛哦~",
        true,
        0,
        ReminderMode.Countdown,
        ReminderRepeat.Interval,
        null,
        null,
        null,
        40,
        CountdownUnit.Minutes,
        false);

    public static int CountTextElements(string text) =>
        new StringInfo(text ?? string.Empty).LengthInTextElements;

    public static string TrimTextElements(string? text, int maximum)
    {
        if (string.IsNullOrEmpty(text) || maximum <= 0)
            return string.Empty;
        var offsets = StringInfo.ParseCombiningCharacters(text);
        return offsets.Length <= maximum ? text : text[..offsets[maximum]];
    }

    public static ReminderValidationError Validate(ReminderDefinition value)
    {
        var text = value.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
            return ReminderValidationError.EmptyText;
        if (CountTextElements(text) > MaxTextElements)
            return ReminderValidationError.TextTooLong;

        return value.Mode switch
        {
            ReminderMode.Countdown => ValidateCountdown(value),
            ReminderMode.Scheduled => ValidateSchedule(value),
            _ => ReminderValidationError.InvalidSchedule
        };
    }

    public static TimeSpan GetCountdownDuration(ReminderDefinition value)
    {
        if (ValidateCountdown(value) != ReminderValidationError.None)
            throw new ArgumentOutOfRangeException(nameof(value), "Countdown is invalid.");

        return value.CountdownUnit switch
        {
            CountdownUnit.Minutes => TimeSpan.FromMinutes(value.CountdownValue),
            CountdownUnit.Hours => TimeSpan.FromHours(value.CountdownValue),
            CountdownUnit.Days => TimeSpan.FromDays(value.CountdownValue),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }

    private static ReminderValidationError ValidateCountdown(ReminderDefinition value)
    {
        if (value.Repeat is not (ReminderRepeat.Once or ReminderRepeat.Interval))
            return ReminderValidationError.InvalidRepeat;
        if (value.CountdownValue <= 0)
            return ReminderValidationError.InvalidCountdown;

        var withinRange = value.CountdownUnit switch
        {
            CountdownUnit.Minutes => value.CountdownValue <= 365 * 24 * 60,
            CountdownUnit.Hours => value.CountdownValue <= 365 * 24,
            CountdownUnit.Days => value.CountdownValue <= 365,
            _ => false
        };
        return withinRange
            ? ReminderValidationError.None
            : ReminderValidationError.InvalidCountdown;
    }

    private static ReminderValidationError ValidateSchedule(ReminderDefinition value)
    {
        if (value.ScheduledTime is null)
            return ReminderValidationError.InvalidSchedule;

        return value.Repeat switch
        {
            ReminderRepeat.Once when value.ScheduledDate is not null =>
                ReminderValidationError.None,
            ReminderRepeat.Daily or ReminderRepeat.Workdays =>
                ReminderValidationError.None,
            ReminderRepeat.Weekly when value.WeeklyDay is not null =>
                ReminderValidationError.None,
            ReminderRepeat.Once or ReminderRepeat.Weekly =>
                ReminderValidationError.InvalidSchedule,
            _ => ReminderValidationError.InvalidRepeat
        };
    }
}
