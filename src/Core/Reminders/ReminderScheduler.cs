namespace DesktopPet.Core.Reminders;

public interface IReminderClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemReminderClock : IReminderClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed record DueReminder(
    Guid Id,
    DateTimeOffset DueAt,
    int Order,
    bool DisableAfterTrigger);

public sealed class ReminderScheduler
{
    private readonly IReminderClock _clock;
    private readonly TimeZoneInfo _timeZone;
    private readonly Dictionary<Guid, ReminderDefinition> _definitions = new();
    private readonly Dictionary<Guid, DateTimeOffset> _dueAt = new();
    private readonly HashSet<Guid> _awaitingAcknowledgement = new();

    public ReminderScheduler(IReminderClock clock, TimeZoneInfo timeZone)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));
    }

    public bool Running { get; private set; }

    public void Start(ReminderDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _definitions.Clear();
        _dueAt.Clear();
        _awaitingAcknowledgement.Clear();
        Running = true;

        foreach (var item in document.Items)
        {
            _definitions[item.Id] = item;
            if (item.Enabled)
                ScheduleFromNow(item);
        }
    }

    public void Stop()
    {
        Running = false;
        _dueAt.Clear();
        _awaitingAcknowledgement.Clear();
    }

    public void Synchronize(ReminderDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var incoming = document.Items.ToDictionary(item => item.Id);

        foreach (var removed in _definitions.Keys.Except(incoming.Keys).ToArray())
        {
            _definitions.Remove(removed);
            _dueAt.Remove(removed);
            _awaitingAcknowledgement.Remove(removed);
        }

        foreach (var item in document.Items)
        {
            var unchanged = _definitions.TryGetValue(item.Id, out var previous) && previous == item;
            _definitions[item.Id] = item;

            if (!Running || !item.Enabled)
            {
                _dueAt.Remove(item.Id);
                _awaitingAcknowledgement.Remove(item.Id);
                continue;
            }

            if (unchanged && (_dueAt.ContainsKey(item.Id) || _awaitingAcknowledgement.Contains(item.Id)))
                continue;

            _dueAt.Remove(item.Id);
            _awaitingAcknowledgement.Remove(item.Id);
            ScheduleFromNow(item);
        }
    }

    public IReadOnlyList<DueReminder> Poll()
    {
        if (!Running)
            return Array.Empty<DueReminder>();

        var now = _clock.UtcNow;
        var due = _dueAt
            .Where(pair => pair.Value <= now)
            .Select(pair => new DueReminder(
                pair.Key,
                pair.Value,
                _definitions[pair.Key].Order,
                _definitions[pair.Key].Repeat == ReminderRepeat.Once))
            .OrderBy(item => item.DueAt)
            .ThenBy(item => item.Order)
            .ToArray();

        foreach (var occurrence in due)
        {
            _dueAt.Remove(occurrence.Id);
            var definition = _definitions[occurrence.Id];
            if (definition.Repeat == ReminderRepeat.Interval)
                _awaitingAcknowledgement.Add(occurrence.Id);
            else if (definition.Repeat != ReminderRepeat.Once)
                ScheduleFromNow(definition);
        }

        return due;
    }

    public void Acknowledge(Guid id)
    {
        if (!Running || !_awaitingAcknowledgement.Remove(id))
            return;
        if (_definitions.TryGetValue(id, out var definition) && definition.Enabled)
            _dueAt[id] = _clock.UtcNow + ReminderScheduleCalculator.CountdownDuration(definition);
    }

    public void ResetAfterResume()
    {
        if (!Running)
            return;

        var document = new ReminderDocument(
            ReminderDocument.CurrentVersion,
            _definitions.Values.OrderBy(item => item.Order).ToArray());
        Start(document);
    }

    private void ScheduleFromNow(ReminderDefinition item)
    {
        if (ReminderDefinition.Validate(item) != ReminderValidationError.None)
            return;

        if (item.Mode == ReminderMode.Countdown)
        {
            _dueAt[item.Id] = _clock.UtcNow + ReminderScheduleCalculator.CountdownDuration(item);
            return;
        }

        var next = ReminderScheduleCalculator.NextScheduledDue(item, _clock.UtcNow, _timeZone);
        if (next is not null)
            _dueAt[item.Id] = next.Value;
    }
}
