using DesktopPet.App.Reminders;
using DesktopPet.Core.Reminders;
using Godot;
using System.Linq;
using System.Threading.Tasks;

namespace DesktopPet.Diagnostics;

public partial class ReminderVisualCapture : Node
{
    private ReminderEditorView _editor = null!;

    public override void _Ready()
    {
        GetWindow().ContentScaleSize = new Vector2I(720, 620);
        GetWindow().ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        GetWindow().ContentScaleAspect = Window.ContentScaleAspectEnum.Ignore;
        GetWindow().Size = new Vector2I(720, 620);
        GetWindow().TransparentBg = true;
        RenderingServer.SetDefaultClearColor(new Color(0, 0, 0, 0));

        _editor = new ReminderEditorView
        {
            Position = new Vector2(40, 40),
            Size = new Vector2(640, 540)
        };
        AddChild(_editor);
        CallDeferred(MethodName.Run);
    }

    private async void Run()
    {
        await WaitFrames(10);
        var diagnosticDirectory = ProjectSettings.GlobalizePath("res://analysis/reminder_visual_v1");
        DirAccess.MakeDirRecursiveAbsolute(diagnosticDirectory);
        Save("reminder_list");

        _editor.BeginNewReminder();
        await WaitFrames(8);
        Save("reminder_edit");

        _editor.SetDraftText(string.Concat(Enumerable.Repeat("萤", 200)));
        await WaitFrames(8);
        Save("reminder_200_chars");

        _editor.Visible = false;
        var bubble = new ReminderBubbleView
        {
            Position = new Vector2(136, 170),
            Size = new Vector2(448, 260)
        };
        AddChild(bubble);
        bubble.SetText("流萤提醒亲爱的，记得站起来运动运动，提提肛哦~");
        bubble.SetPlacement(CapturePlacement(BubbleSide.Above, 448, 260));
        await WaitFrames(10);
        Save("reminder_bubble");
        Save("reminder_bubble_above");

        bubble.SetPlacement(CapturePlacement(BubbleSide.UpperRight, 448, 260));
        await WaitFrames(5);
        Save("reminder_bubble_right");

        bubble.SetPlacement(CapturePlacement(BubbleSide.UpperLeft, 448, 260));
        await WaitFrames(5);
        Save("reminder_bubble_left");

        bubble.SetPlacement(CapturePlacement(BubbleSide.Below, 448, 260));
        await WaitFrames(5);
        Save("reminder_bubble_below");

        bubble.Position = new Vector2(136, 100);
        bubble.Size = new Vector2(448, 388);
        bubble.SetText(string.Concat(Enumerable.Repeat("萤", 200)));
        bubble.SetPlacement(CapturePlacement(BubbleSide.Above, 448, 388));
        await WaitFrames(8);
        Save("reminder_bubble_200_chars");
        GetTree().Quit();
    }

    private static BubblePlacement CapturePlacement(BubbleSide side, int width, int height) =>
        new(
            new ScreenRect(0, 0, width, height),
            side,
            width / 2,
            height / 2);

    private async Task WaitFrames(int count)
    {
        for (var index = 0; index < count; index++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Save(string name)
    {
        var image = GetViewport().GetTexture().GetImage();
        var directory = ProjectSettings.GlobalizePath("res://analysis/reminder_visual_v1");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        var error = image.SavePng($"{directory}/{name}.png");
        if (error != Error.Ok)
            GD.PushError($"Failed to save reminder capture {name}: {error}");
    }
}
