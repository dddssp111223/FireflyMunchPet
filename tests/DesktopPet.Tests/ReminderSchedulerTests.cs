using DesktopPet.Core.Reminders;

namespace DesktopPet.Tests;

internal static class ReminderSchedulerTests
{
    private sealed class FakeClock(DateTimeOffset now) : IReminderClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    public static void Run()
    {
        IntervalWaitsForAcknowledgement();
        StopAndRestartResetCountdown();
        CalendarSchedulesChooseFutureOccurrences();
        DueItemsUseStableOrder();
        SynchronizePreservesOnlyUnchangedCountdowns();
        ResumeResetsCountdowns();
    }

    private static void IntervalWaitsForAcknowledgement()
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 8, 3, 1, 0, 0, TimeSpan.Zero));
        var scheduler = new ReminderScheduler(clock, TimeZoneInfo.Utc);
        var item = ReminderDefinition.CreateDefault();
        scheduler.Start(new ReminderDocument(1, new[] { item }));

        clock.UtcNow = clock.UtcNow.AddMinutes(39);
        AssertEx.Equal(0, scheduler.Poll().Count, "not due before forty minutes");
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        AssertEx.Equal(item.Id, scheduler.Poll().Single().Id, "due at forty minutes");
        clock.UtcNow = clock.UtcNow.AddHours(2);
        AssertEx.Equal(0, scheduler.Poll().Count, "interval waits for acknowledgement");
        scheduler.Acknowledge(item.Id);
        clock.UtcNow = clock.UtcNow.AddMinutes(40);
        AssertEx.Equal(item.Id, scheduler.Poll().Single().Id, "next interval starts after acknowledgement");
    }

    private static void StopAndRestartResetCountdown()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var item = ReminderDefinition.CreateDefault() with { CountdownValue = 10 };
        var document = new ReminderDocument(1, new[] { item });
        var scheduler = new ReminderScheduler(clock, TimeZoneInfo.Utc);
        scheduler.Start(document);
        clock.UtcNow = clock.UtcNow.AddMinutes(9);
        scheduler.Stop();
        clock.UtcNow = clock.UtcNow.AddHours(3);
        AssertEx.Equal(0, scheduler.Poll().Count, "stopped scheduler stays silent");
        scheduler.Start(document);
        clock.UtcNow = clock.UtcNow.AddMinutes(9);
        AssertEx.Equal(0, scheduler.Poll().Count, "restart uses full countdown");
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        AssertEx.Equal(1, scheduler.Poll().Count, "restart fires after full countdown");
    }

    private static void CalendarSchedulesChooseFutureOccurrences()
    {
        var now = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero);
        var template = ReminderDefinition.CreateDefault() with
        {
            Mode = ReminderMode.Scheduled,
            Repeat = ReminderRepeat.Daily,
            ScheduledTime = new TimeOnly(9, 0),
            ScheduledDate = null,
            WeeklyDay = null,
            CountdownValue = 0
        };

        AssertEx.Equal(
            new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero),
            ReminderScheduleCalculator.NextScheduledDue(template, now, TimeZoneInfo.Utc),
            "daily reminder chooses tomorrow");

        var fridayEvening = new DateTimeOffset(2026, 8, 7, 18, 0, 0, TimeSpan.Zero);
        AssertEx.Equal(
            new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero),
            ReminderScheduleCalculator.NextScheduledDue(
                template with { Repeat = ReminderRepeat.Workdays },
                fridayEvening,
                TimeZoneInfo.Utc),
            "workday reminder skips weekend");

        AssertEx.Equal(
            new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero),
            ReminderScheduleCalculator.NextScheduledDue(
                template with { Repeat = ReminderRepeat.Weekly, WeeklyDay = DayOfWeek.Monday },
                now,
                TimeZoneInfo.Utc),
            "weekly reminder chooses next week");

        AssertEx.Equal<DateTimeOffset?>(
            null,
            ReminderScheduleCalculator.NextScheduledDue(
                template with
                {
                    Repeat = ReminderRepeat.Once,
                    ScheduledDate = new DateOnly(2026, 8, 2)
                },
                now,
                TimeZoneInfo.Utc),
            "past one-shot has no future occurrence");
    }

    private static void DueItemsUseStableOrder()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var first = ReminderDefinition.CreateDefault() with
        {
            Id = Guid.NewGuid(),
            Order = 0,
            CountdownValue = 5
        };
        var second = first with { Id = Guid.NewGuid(), Order = 1 };
        var scheduler = new ReminderScheduler(clock, TimeZoneInfo.Utc);
        scheduler.Start(new ReminderDocument(1, new[] { second, first }));
        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        var due = scheduler.Poll();
        AssertEx.Equal(first.Id, due[0].Id, "first list order wins tie");
        AssertEx.Equal(second.Id, due[1].Id, "second list order follows");
    }

    private static void SynchronizePreservesOnlyUnchangedCountdowns()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var first = ReminderDefinition.CreateDefault() with
        {
            Id = Guid.NewGuid(), Order = 0, CountdownValue = 10
        };
        var second = first with { Id = Guid.NewGuid(), Order = 1, CountdownValue = 20 };
        var scheduler = new ReminderScheduler(clock, TimeZoneInfo.Utc);
        scheduler.Start(new ReminderDocument(1, new[] { first, second }));
        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        scheduler.Synchronize(new ReminderDocument(1, new[]
        {
            first,
            second with { Text = "changed" }
        }));

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        AssertEx.Equal(first.Id, scheduler.Poll().Single().Id, "unchanged countdown is preserved");
        clock.UtcNow = clock.UtcNow.AddMinutes(14);
        AssertEx.Equal(0, scheduler.Poll().Count, "changed countdown was reset");
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        AssertEx.Equal(second.Id, scheduler.Poll().Single().Id, "changed countdown uses new start");
    }

    private static void ResumeResetsCountdowns()
    {
        var clock = new FakeClock(DateTimeOffset.UnixEpoch);
        var item = ReminderDefinition.CreateDefault() with { CountdownValue = 10 };
        var scheduler = new ReminderScheduler(clock, TimeZoneInfo.Utc);
        scheduler.Start(new ReminderDocument(1, new[] { item }));
        clock.UtcNow = clock.UtcNow.AddHours(1);
        scheduler.ResetAfterResume();
        AssertEx.Equal(0, scheduler.Poll().Count, "resume does not backfill overdue countdown");
        clock.UtcNow = clock.UtcNow.AddMinutes(10);
        AssertEx.Equal(item.Id, scheduler.Poll().Single().Id, "resume restarts full countdown");
    }
}
