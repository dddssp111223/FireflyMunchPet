using DesktopPet.Character;
using Godot;
using System.Threading.Tasks;

namespace DesktopPet.Diagnostics;

public partial class VisualCapture : Node2D
{
    private CharacterRig _rig = null!;
    private EyeRig _eyeRig = null!;

    public override void _Ready()
    {
        _rig = GetNode<CharacterRig>("CharacterRig");
        _eyeRig = GetNode<EyeRig>("CharacterRig/CharacterMotionRoot/EyeRig");
        CallDeferred(MethodName.Run);
    }

    private async void Run()
    {
        _rig.SetEyeTrackingEnabled(false);
        _rig.SetIdleMotionEnabled(false);
        await WaitFrames(12);
        Save("idle");

        foreach (var percent in new[] { 30, 50, 75, 100, 125, 150 })
        {
            var size = Mathf.RoundToInt(512 * percent / 100f);
            GetWindow().Size = new Vector2I(size, size);
            await WaitFrames(10);
            Save($"scale_{percent}");
        }
        GetWindow().Size = new Vector2I(512, 512);
        await WaitFrames(10);

        _eyeRig.SetTarget(new Vector2(360, -220));
        await WaitFrames(16);
        Save("eye_upper_right");

        _eyeRig.SetTarget(new Vector2(-360, 160));
        await WaitFrames(16);
        Save("eye_lower_left");

        _rig.SetFileHover(true);
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        Save("star");
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        Save("star_late");

        _rig.SetFileHover(false);
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        _rig.SetEyeTrackingEnabled(false);

        _rig.PlayClickBounce();
        await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
        Save("click_squash");
        await ToSignal(GetTree().CreateTimer(0.36), SceneTreeTimer.SignalName.Timeout);
        Save("click_restored");

        _rig.PlaySwallow(System.Array.Empty<Texture2D>(), new Vector2(420, 180));
        await ToSignal(GetTree().CreateTimer(0.72), SceneTreeTimer.SignalName.Timeout);
        Save("swallow_gulp");
        await ToSignal(GetTree().CreateTimer(0.65), SceneTreeTimer.SignalName.Timeout);
        Save("swallow_restored");

        _rig.SetHarmonizedMode(true);
        _rig.SetEyeTrackingEnabled(false);
        await WaitFrames(12);
        Save("harmonized_idle");

        _eyeRig.SetTarget(new Vector2(360, -220));
        await WaitFrames(16);
        Save("harmonized_eye_upper_right");
        _eyeRig.SetTarget(new Vector2(-360, 160));
        await WaitFrames(16);
        Save("harmonized_eye_lower_left");

        _rig.SetFileHover(true);
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_star");
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_star_late");
        _rig.SetFileHover(false);
        await ToSignal(GetTree().CreateTimer(0.18), SceneTreeTimer.SignalName.Timeout);
        _rig.SetEyeTrackingEnabled(false);

        _rig.PlayClickBounce();
        await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_click_squash");
        await ToSignal(GetTree().CreateTimer(0.36), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_click_restored");

        _rig.PlaySwallow(System.Array.Empty<Texture2D>(), new Vector2(420, 180));
        await ToSignal(GetTree().CreateTimer(0.72), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_gulp");
        await ToSignal(GetTree().CreateTimer(0.65), SceneTreeTimer.SignalName.Timeout);
        Save("harmonized_swallow_restored");

        GetTree().Quit();
    }

    private async Task WaitFrames(int count)
    {
        for (var frame = 0; frame < count; frame++)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void Save(string name)
    {
        var image = GetViewport().GetTexture().GetImage();
        var directory = ProjectSettings.GlobalizePath("res://analysis/visual_v7");
        DirAccess.MakeDirRecursiveAbsolute(directory);
        var result = image.SavePng($"{directory}/{name}.png");
        if (result != Error.Ok)
            GD.PushError($"Failed to save visual capture {name}: {result}");
    }
}
