using DesktopPet.Core.Reminders;
using Godot;
using System;
using System.Linq;

namespace DesktopPet.App.Reminders;

public partial class ReminderEditorWindow : Window
{
    public event Action<ReminderDocument>? DocumentChanged;

    private ReminderDocument _document = ReminderDocument.CreateDefault();
    private ReminderEditorView _view = null!;
    private ConfirmationDialog _deleteDialog = null!;
    private Guid? _pendingDelete;

    public override void _Ready()
    {
        Title = "提醒备忘录";
        Size = new Vector2I(620, 520);
        MinSize = new Vector2I(540, 480);
        Unresizable = false;
        Borderless = false;
        Exclusive = false;
        Transient = false;
        CloseRequested += Hide;

        _view = new ReminderEditorView();
        AddChild(_view);
        _view.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _view.SaveRequested += SaveItem;
        _view.TaskEnabledChanged += ToggleItem;
        _view.DeleteRequested += ConfirmDelete;

        _deleteDialog = new ConfirmationDialog
        {
            Title = "删除提醒事项",
            DialogText = "确定删除这个提醒事项吗？",
            OkButtonText = "删除"
        };
        _deleteDialog.AddCancelButton("取消");
        _deleteDialog.Confirmed += DeleteConfirmed;
        AddChild(_deleteDialog);
        Hide();
    }

    public void Initialize(ReminderDocument document)
    {
        _document = document;
        if (IsNodeReady())
            _view.SetDocument(document);
    }

    public void ReplaceDocument(ReminderDocument document)
    {
        _document = document;
        _view.SetDocument(document);
    }

    public void ShowOrFocus()
    {
        if (Visible)
        {
            GrabFocus();
            return;
        }

        PopupCentered(new Vector2I(620, 520));
        GrabFocus();
    }

    private void SaveItem(ReminderDefinition item)
    {
        _document = _document.Upsert(item);
        _view.SetDocument(_document);
        _view.ShowTaskList();
        DocumentChanged?.Invoke(_document);
    }

    private void ToggleItem(Guid id, bool enabled)
    {
        var existing = _document.Items.FirstOrDefault(item => item.Id == id);
        if (existing is null || existing.Enabled == enabled)
            return;
        _document = _document.Upsert(existing with { Enabled = enabled, HasTriggered = false });
        _view.SetDocument(_document);
        DocumentChanged?.Invoke(_document);
    }

    private void ConfirmDelete(Guid id)
    {
        _pendingDelete = id;
        _deleteDialog.PopupCentered();
    }

    private void DeleteConfirmed()
    {
        if (_pendingDelete is null)
            return;
        _document = _document.Remove(_pendingDelete.Value);
        _pendingDelete = null;
        _view.SetDocument(_document);
        _view.ShowTaskList();
        DocumentChanged?.Invoke(_document);
    }
}
