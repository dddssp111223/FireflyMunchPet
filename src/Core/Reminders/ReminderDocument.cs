namespace DesktopPet.Core.Reminders;

public sealed record ReminderDocument
{
    public const int CurrentVersion = 1;
    public const int MaxItems = 5;

    public int Version { get; init; }
    public IReadOnlyList<ReminderDefinition> Items { get; init; }

    public ReminderDocument(int version, IReadOnlyList<ReminderDefinition> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count > MaxItems)
            throw new InvalidOperationException("At most five reminders are allowed.");

        Version = version;
        Items = items.ToArray();
    }

    public static ReminderDocument CreateDefault() =>
        new(CurrentVersion, new[] { ReminderDefinition.CreateDefault() });

    public int NextOrder() => Items.Count == 0 ? 0 : Items.Max(item => item.Order) + 1;

    public ReminderDocument Upsert(ReminderDefinition item)
    {
        if (ReminderDefinition.Validate(item) != ReminderValidationError.None)
            throw new ArgumentException("Reminder is invalid.", nameof(item));

        var next = Items
            .Where(existing => existing.Id != item.Id)
            .Append(item)
            .OrderBy(existing => existing.Order)
            .ToArray();
        return new ReminderDocument(CurrentVersion, next);
    }

    public ReminderDocument Remove(Guid id) =>
        new(CurrentVersion, Items.Where(item => item.Id != id).ToArray());
}
