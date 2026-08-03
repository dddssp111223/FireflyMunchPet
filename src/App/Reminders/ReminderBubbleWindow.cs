using DesktopPet.Core.Reminders;
using Godot;
using System;

namespace DesktopPet.App.Reminders;

public partial class ReminderBubbleWindow : Window
{
    public event Action<Guid>? Acknowledged;

    private ReminderBubbleView _view = null!;
    private Guid? _activeReminderId;
    private Rect2I _lastPetRect;
    private Rect2I _lastWorkArea;

    public static ReminderBubbleWindow CreateNative(string name) => new()
    {
        Name = name,
        Title = "提醒",
        Transparent = true,
        TransparentBg = true,
        Borderless = true,
        Unresizable = true,
        AlwaysOnTop = false,
        Exclusive = false,
        Transient = false,
        Size = new Vector2I(448, 238),
        MinSize = new Vector2I(400, 220)
    };

    public override void _Ready()
    {
        _view = new ReminderBubbleView();
        AddChild(_view);
        _view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _view.Acknowledged += HandleAcknowledged;
        CloseRequested += CloseReminder;
        Hide();
    }

    public void ShowReminder(
        ReminderDefinition item,
        Rect2I petRect,
        Rect2I workArea,
        bool alwaysOnTop)
    {
        _activeReminderId = item.Id;
        _lastPetRect = petRect;
        _lastWorkArea = workArea;
        AlwaysOnTop = alwaysOnTop;
        var textElements = ReminderDefinition.CountTextElements(item.Text);
        Size = new Vector2I(
            448,
            Math.Clamp(238 + Math.Max(0, textElements - 48) / 12 * 20, 238, 388));
        _view.SetText(item.Text);
        Reposition(petRect, workArea);
        Show();
        GrabFocus();
    }

    public void Reposition(Rect2I petRect, Rect2I workArea)
    {
        _lastPetRect = petRect;
        _lastWorkArea = workArea;
        var placement = ReminderBubblePlacement.Calculate(
            ToScreenRect(petRect),
            Size.X,
            Size.Y,
            ToScreenRect(workArea),
            12);
        Position = new Vector2I(placement.Bounds.X, placement.Bounds.Y);
        _view.SetPlacement(placement);
    }

    public void RefreshPosition()
    {
        if (_activeReminderId is not null)
            Reposition(_lastPetRect, _lastWorkArea);
    }

    public void CloseReminder()
    {
        _activeReminderId = null;
        Hide();
    }

    private void HandleAcknowledged()
    {
        if (_activeReminderId is not { } id)
            return;
        CloseReminder();
        Acknowledged?.Invoke(id);
    }

    private static ScreenRect ToScreenRect(Rect2I value) =>
        new(value.Position.X, value.Position.Y, value.Size.X, value.Size.Y);
}
