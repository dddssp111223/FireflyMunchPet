using DesktopPet.App.Reminders;
using DesktopPet.Core.Reminders;
using Godot;
using System.IO;
using System.Threading.Tasks;

namespace DesktopPet.Diagnostics;

public partial class ReminderNativeWindowProbe : Node
{
    private ReminderEditorWindow _editor = null!;
    private ReminderBubbleWindow _bubble = null!;
    private Rect2I _workArea;

    public override void _Ready()
    {
        GetTree().Root.GuiEmbedSubwindows = false;

        var document = ReminderDocument.CreateDefault();
        _editor = new ReminderEditorWindow { Name = "NativeEditorProbe" };
        _bubble = ReminderBubbleWindow.CreateNative("NativeBubbleProbe");
        AddChild(_editor);
        AddChild(_bubble);

        _editor.Initialize(document);
        _editor.ShowOrFocus();

        _workArea = DisplayServer.ScreenGetUsableRect(DisplayServer.GetPrimaryScreen());
        var petSize = new Vector2I(160, 160);
        var petPosition = _workArea.End - petSize - new Vector2I(20, 20);
        _bubble.ShowReminder(
            document.Items[0],
            new Rect2I(petPosition, petSize),
            _workArea,
            false);

        CallDeferred(MethodName.ValidateNativeWindows);
    }

    private async void ValidateNativeWindows()
    {
        await WaitFrames(20);

        var rootId = GetWindow().GetWindowId();
        var editorId = _editor.GetWindowId();
        var bubbleId = _bubble.GetWindowId();
        var bubbleRect = new Rect2I(_bubble.Position, _bubble.Size);
        var distinctNativeWindows = editorId != rootId && bubbleId != rootId && editorId != bubbleId;

        if (!distinctNativeWindows || !_workArea.Encloses(bubbleRect))
        {
            var failure =
                $"Reminder windows are embedded or clipped: root={rootId}, editor={editorId}, " +
                $"bubble={bubbleId}, bubbleRect={bubbleRect}, workArea={_workArea}";
            WriteResult($"FAIL {failure}");
            GD.PushError(failure);
            CloseProbe();
            GetTree().Quit(1);
            return;
        }

        var success =
            $"Native reminder windows passed: root={rootId}, editor={editorId}, " +
            $"editorSize={_editor.Size}, bubble={bubbleId}, bubbleRect={bubbleRect}, " +
            $"workArea={_workArea}";
        WriteResult($"PASS {success}");
        GD.Print(success);
        CloseProbe();
        GetTree().Quit();
    }

    private async Task WaitFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void CloseProbe()
    {
        _bubble.CloseReminder();
        _editor.Hide();
    }

    private static void WriteResult(string result)
    {
        var path = ProjectSettings.GlobalizePath("user://native-window-probe.txt");
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, result);
    }
}
