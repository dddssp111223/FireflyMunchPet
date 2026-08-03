using DesktopPet.Character;
using DesktopPet.Core.Reminders;
using Godot;
using System;
using System.Diagnostics;
using System.Linq;

namespace DesktopPet.App.Reminders;

public partial class ReminderCoordinator : Node
{
    private readonly ReminderQueue _queue = new();
    private readonly Stopwatch _frameGap = Stopwatch.StartNew();
    private ReminderRepository _repository = null!;
    private ReminderScheduler _scheduler = null!;
    private ReminderDocument _document = null!;
    private Window _petWindow = null!;
    private CharacterRig _rig = null!;
    private Func<bool> _tryBeginReminderBounce = null!;
    private Func<bool> _canPresentReminder = null!;
    private ReminderEditorWindow _editor = null!;
    private ReminderBubbleWindow _bubble = null!;
    private Guid? _activeReminderId;
    private bool _enabled;
    private double _pollAccumulator;

    public void Initialize(
        string reminderPath,
        Window petWindow,
        CharacterRig rig,
        Func<bool> tryBeginReminderBounce,
        Func<bool> canPresentReminder)
    {
        _repository = new ReminderRepository(reminderPath);
        _document = _repository.LoadOrCreate();
        _scheduler = new ReminderScheduler(new SystemReminderClock(), TimeZoneInfo.Local);
        var normalized = ReminderMaintenance.DisableExpiredOneShots(
            _document,
            DateTimeOffset.UtcNow,
            TimeZoneInfo.Local);
        if (!normalized.Items.SequenceEqual(_document.Items))
        {
            _document = normalized;
            _repository.Save(_document);
        }
        _petWindow = petWindow;
        _rig = rig;
        _tryBeginReminderBounce = tryBeginReminderBounce;
        _canPresentReminder = canPresentReminder;

        _editor = new ReminderEditorWindow { Name = "ReminderEditorWindow" };
        AddChild(_editor);
        _editor.Initialize(_document);
        _editor.DocumentChanged += ApplyEditedDocument;

        _bubble = ReminderBubbleWindow.CreateNative("ReminderBubbleWindow");
        AddChild(_bubble);
        _bubble.Acknowledged += Acknowledge;
        _frameGap.Restart();
    }

    public override void _Process(double delta)
    {
        var processGap = _frameGap.Elapsed;
        _frameGap.Restart();
        if (!_enabled)
            return;

        if (processGap >= TimeSpan.FromSeconds(30))
        {
            ResetAfterResume();
            return;
        }

        _pollAccumulator += delta;
        if (_pollAccumulator >= 0.25)
        {
            _pollAccumulator = 0;
            var due = _scheduler.Poll();
            if (due.Count > 0)
            {
                _queue.Enqueue(due);
                DisableTriggeredOneShots(due);
            }
        }

        TryPresentNext();
    }

    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _pollAccumulator = 0;
        if (enabled)
        {
            _scheduler.Start(_document);
            _frameGap.Restart();
            return;
        }

        _scheduler.Stop();
        _queue.Clear();
        _activeReminderId = null;
        _bubble.CloseReminder();
    }

    public void OpenEditor()
    {
        _editor.ReplaceDocument(_document);
        _editor.ShowOrFocus();
    }

    public void SetTopmost(bool alwaysOnTop)
    {
        _bubble.AlwaysOnTop = alwaysOnTop;
    }

    public void NotifyPetMovedOrScaled()
    {
        if (_activeReminderId is null)
            return;
        var (petRect, workArea) = CurrentRects();
        _bubble.Reposition(petRect, workArea);
    }

    public void ResetAfterResume()
    {
        if (!_enabled)
            return;
        _queue.Clear();
        _activeReminderId = null;
        _bubble.CloseReminder();
        _scheduler.ResetAfterResume();
        _frameGap.Restart();
    }

    public void Shutdown()
    {
        _enabled = false;
        _scheduler.Stop();
        _queue.Clear();
        _bubble.CloseReminder();
        _editor.Hide();
    }

    private void ApplyEditedDocument(ReminderDocument document)
    {
        var activeDefinition = _activeReminderId is { } activeId
            ? document.Items.FirstOrDefault(item => item.Id == activeId)
            : null;
        if (_activeReminderId is not null && (activeDefinition is null || !activeDefinition.Enabled))
        {
            _bubble.CloseReminder();
            _activeReminderId = null;
        }

        foreach (var queued in _document.Items)
        {
            var replacement = document.Items.FirstOrDefault(item => item.Id == queued.Id);
            if (replacement is null || !replacement.Enabled)
                _queue.Remove(queued.Id);
        }

        _document = document;
        _repository.Save(_document);
        _scheduler.Synchronize(_document);
        _editor.ReplaceDocument(_document);
    }

    private void DisableTriggeredOneShots(System.Collections.Generic.IReadOnlyList<DueReminder> due)
    {
        var changed = false;
        foreach (var occurrence in due.Where(item => item.DisableAfterTrigger))
        {
            var definition = _document.Items.FirstOrDefault(item => item.Id == occurrence.Id);
            if (definition is null)
                continue;
            _document = _document.Upsert(definition with { Enabled = false, HasTriggered = true });
            changed = true;
        }

        if (!changed)
            return;
        _repository.Save(_document);
        _scheduler.Synchronize(_document);
        _editor.ReplaceDocument(_document);
    }

    private void TryPresentNext()
    {
        if (_activeReminderId is not null || !_canPresentReminder())
            return;

        while (_queue.Current is { } current)
        {
            var definition = _document.Items.FirstOrDefault(item => item.Id == current.Id);
            if (definition is null)
            {
                _queue.AcknowledgeCurrent();
                continue;
            }
            if (!_tryBeginReminderBounce())
                return;

            _activeReminderId = current.Id;
            var (petRect, workArea) = CurrentRects();
            _bubble.ShowReminder(definition, petRect, workArea, _petWindow.AlwaysOnTop);
            _rig.PlayReminderBounceSequence();
            return;
        }
    }

    private void Acknowledge(Guid id)
    {
        if (_activeReminderId != id)
            return;
        _scheduler.Acknowledge(id);
        _queue.AcknowledgeCurrent();
        _activeReminderId = null;
    }

    private (Rect2I Pet, Rect2I WorkArea) CurrentRects()
    {
        var petRect = new Rect2I(_petWindow.Position, _petWindow.Size);
        var screen = DisplayServer.GetScreenFromRect(new Rect2(
            petRect.Position.X,
            petRect.Position.Y,
            petRect.Size.X,
            petRect.Size.Y));
        var workArea = DisplayServer.ScreenGetUsableRect(screen);
        return (petRect, workArea);
    }
}
