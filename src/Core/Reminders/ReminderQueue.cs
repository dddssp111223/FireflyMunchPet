namespace DesktopPet.Core.Reminders;

public sealed class ReminderQueue
{
    private readonly List<DueReminder> _items = new();

    public DueReminder? Current => _items.Count == 0 ? null : _items[0];
    public int Count => _items.Count;

    public void Enqueue(IEnumerable<DueReminder> items)
    {
        foreach (var item in items)
        {
            if (_items.All(existing => existing.Id != item.Id))
                _items.Add(item);
        }

        _items.Sort((left, right) =>
        {
            var byTime = left.DueAt.CompareTo(right.DueAt);
            return byTime != 0 ? byTime : left.Order.CompareTo(right.Order);
        });
    }

    public DueReminder? AcknowledgeCurrent()
    {
        if (_items.Count == 0)
            return null;
        var current = _items[0];
        _items.RemoveAt(0);
        return current;
    }

    public void Remove(Guid id) => _items.RemoveAll(item => item.Id == id);

    public void Clear() => _items.Clear();
}
