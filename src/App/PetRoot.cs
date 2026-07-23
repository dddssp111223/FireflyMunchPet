using DesktopPet.Character;
using DesktopPet.Core;
using DesktopPet.Windows;
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NumericsVector2 = System.Numerics.Vector2;

namespace DesktopPet.App;

public partial class PetRoot : Node2D
{
    private const int MenuTopmost = 1;
    private const int MenuScale75 = 75;
    private const int MenuScale100 = 100;
    private const int MenuScale125 = 125;
    private const int MenuScale150 = 150;
    private const int MenuMute = 200;
    private const int MenuResetPosition = 300;
    private const int MenuExit = 400;

    private readonly PetStateMachine _state = new();
    private Window _window = null!;
    private CharacterRig _rig = null!;
    private PetAudioController _audio = null!;
    private ShellFileOperation _deleteService = null!;
    private PopupMenu _trayMenu = null!;
    private StatusIndicator _statusIndicator = null!;
    private PetSettings _settings = PetSettings.Default;

    private bool _pointerDown;
    private bool _externalHover;
    private Vector2 _downLocal;
    private Vector2I _downScreen;
    private Vector2I _downWindow;
    private HitRegion _downRegion;
    private GestureKind _activeGesture;

    public override void _Ready()
    {
        _window = GetWindow();
        _rig = GetNode<CharacterRig>("CharacterRig");
        _audio = new PetAudioController(this);

        _window.TransparentBg = true;
        _window.Borderless = true;
        _window.AlwaysOnTop = false;
        _window.Unresizable = true;
        _window.FilesDropped += OnFilesDropped;
        _rig.OneShotFinished += OnOneShotFinished;

        SetMousePassthroughContour();
        LoadSettings();
        ApplySettings(placeAtDefaultWhenUnset: true);
        WindowStyleService.ApplyDesktopPetStyles(_window);
        _deleteService = new ShellFileOperation(WindowStyleService.GetHwnd(_window));
        CreateTray();
    }

    public override void _ExitTree()
    {
        SaveSettings();
        if (IsInstanceValid(_statusIndicator))
            _statusIndicator.Visible = false;
    }

    public override void _Process(double delta)
    {
        PollExternalDragHover();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton button when button.ButtonIndex == MouseButton.Left:
                if (button.Pressed)
                    BeginPointer(button.Position);
                else
                    EndPointer(button.Position);
                GetViewport().SetInputAsHandled();
                break;

            case InputEventMouseMotion motion when _pointerDown:
                UpdatePointer(motion.Position);
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void BeginPointer(Vector2 localPoint)
    {
        if (_state.State != PetState.Idle)
            return;

        _pointerDown = true;
        _downLocal = localPoint;
        _downScreen = DisplayServer.MouseGetPosition();
        _downWindow = _window.Position;
        _downRegion = HitTest(localPoint);
        _activeGesture = GestureKind.None;
    }

    private void UpdatePointer(Vector2 localPoint)
    {
        if (!_pointerDown)
            return;

        var classified = GestureClassifier.Classify(
            _downRegion,
            new NumericsVector2(_downLocal.X, _downLocal.Y),
            new NumericsVector2(localPoint.X, localPoint.Y),
            8);

        if (_activeGesture == GestureKind.None && classified != GestureKind.Click)
        {
            _activeGesture = classified;
            if (_activeGesture == GestureKind.CheekDrag)
                _state.BeginCheekDrag();
            else if (_activeGesture == GestureKind.WindowDrag)
                _state.BeginWindowDrag();
        }

        if (_activeGesture == GestureKind.CheekDrag)
        {
            _rig.SetCheekPull(localPoint - _downLocal);
        }
        else if (_activeGesture == GestureKind.WindowDrag)
        {
            var delta = DisplayServer.MouseGetPosition() - _downScreen;
            _window.Position = ClampToPrimaryWorkArea(_downWindow + delta);
        }
    }

    private void EndPointer(Vector2 localPoint)
    {
        if (!_pointerDown)
            return;

        _pointerDown = false;
        var completedGesture = _activeGesture;
        _activeGesture = GestureKind.None;

        if (completedGesture == GestureKind.CheekDrag)
        {
            _rig.ReleaseCheek();
            _state.FinishTransient();
            return;
        }

        if (completedGesture == GestureKind.WindowDrag)
        {
            _state.FinishTransient();
            SaveCurrentPosition();
            SaveSettings();
            return;
        }

        var click = GestureClassifier.Classify(
            _downRegion,
            new NumericsVector2(_downLocal.X, _downLocal.Y),
            new NumericsVector2(localPoint.X, localPoint.Y),
            8);
        if (click == GestureKind.Click && _state.BeginClickBounce())
        {
            _audio.Play(PetAudioController.Sound.Click);
            _rig.PlayClickBounce();
        }
    }

    private void PollExternalDragHover()
    {
        if (_pointerDown || _state.IsBusy)
            return;

        var leftDown = OperatingSystem.IsWindows() &&
                       (NativeMethods.GetAsyncKeyState(NativeMethods.VkLButton) & 0x8000) != 0;
        var cursor = DisplayServer.MouseGetPosition();
        var rect = new Rect2I(_window.Position, _window.Size);
        var hovering = leftDown && rect.HasPoint(cursor);

        if (hovering && !_externalHover && _state.State == PetState.Idle)
        {
            _externalHover = _state.EnterFileHover();
            if (_externalHover)
                _rig.SetFileHover(true);
        }
        else if (!hovering && _externalHover && _state.State == PetState.FileHover)
        {
            _externalHover = false;
            _state.LeaveFileHover();
            _rig.SetFileHover(false);
        }
    }

    private void OnFilesDropped(string[] files)
    {
        if (_state.IsBusy || files.Length == 0)
            return;

        DropBatch batch;
        try
        {
            var local = GetViewport().GetMousePosition();
            batch = DropBatch.Create(files, (int)local.X, (int)local.Y);
        }
        catch (Exception)
        {
            return;
        }

        if (_state.State == PetState.Idle)
            _state.EnterFileHover();
        if (_state.State != PetState.FileHover || !_state.BeginShellPending())
            return;

        _externalHover = false;
        _rig.SetFileHover(false);

        var icons = new List<Texture2D>();
        foreach (var path in batch.Paths.Take(3))
        {
            var icon = ShellIconService.LoadIcon(path);
            if (icon is not null)
                icons.Add(icon);
        }

        var result = _deleteService.MoveToRecycleBin(batch);
        switch (_state.ResolveDelete(result.Outcome))
        {
            case PetState.Swallowing:
                _audio.Play(PetAudioController.Sound.Suction);
                _audio.Play(PetAudioController.Sound.Gulp);
                _rig.PlaySwallow(icons, new Vector2(batch.DropX, batch.DropY));
                break;
            case PetState.Rejecting:
                _audio.Play(PetAudioController.Sound.Reject);
                _rig.PlayReject();
                break;
            default:
                _rig.SetFileHover(false);
                break;
        }
    }

    private void OnOneShotFinished() => _state.FinishTransient();

    private static HitRegion HitTest(Vector2 point)
    {
        if (point.X >= 320 && point.Y >= 245 && point.Y <= 425)
            return HitRegion.Cheek;
        if (point.X >= 70 && point.X <= 450 && point.Y <= 205)
            return HitRegion.MoveHandle;
        return HitRegion.Visible;
    }

    private void CreateTray()
    {
        _trayMenu = new PopupMenu { Name = "TrayMenu" };
        AddChild(_trayMenu);
        _trayMenu.AddCheckItem("置顶显示", MenuTopmost);
        _trayMenu.AddSeparator();
        _trayMenu.AddRadioCheckItem("缩放 75%", MenuScale75);
        _trayMenu.AddRadioCheckItem("缩放 100%", MenuScale100);
        _trayMenu.AddRadioCheckItem("缩放 125%", MenuScale125);
        _trayMenu.AddRadioCheckItem("缩放 150%", MenuScale150);
        _trayMenu.AddSeparator();
        _trayMenu.AddCheckItem("静音", MenuMute);
        _trayMenu.AddItem("重置到右下角", MenuResetPosition);
        _trayMenu.AddSeparator();
        _trayMenu.AddItem("退出", MenuExit);
        _trayMenu.IdPressed += OnTrayMenuPressed;

        _statusIndicator = new StatusIndicator
        {
            Name = "StatusIndicator",
            Tooltip = "桌宠",
            Icon = GD.Load<Texture2D>("res://assets/character/master_transparent.png"),
            Visible = true
        };
        AddChild(_statusIndicator);
        _statusIndicator.Menu = _trayMenu.GetPath();
        RefreshTrayChecks();
    }

    private void OnTrayMenuPressed(long id)
    {
        switch ((int)id)
        {
            case MenuTopmost:
                _settings = _settings with { AlwaysOnTop = !_settings.AlwaysOnTop };
                _window.AlwaysOnTop = _settings.AlwaysOnTop;
                break;
            case MenuScale75:
            case MenuScale100:
            case MenuScale125:
            case MenuScale150:
                _settings = _settings with { ScalePercent = (int)id };
                ApplyScale();
                break;
            case MenuMute:
                _settings = _settings with { Muted = !_settings.Muted };
                _audio.Muted = _settings.Muted;
                break;
            case MenuResetPosition:
                PlaceAtLowerRight();
                SaveCurrentPosition();
                break;
            case MenuExit:
                _statusIndicator.Visible = false;
                GetTree().Quit();
                return;
        }

        RefreshTrayChecks();
        SaveSettings();
    }

    private void RefreshTrayChecks()
    {
        SetMenuChecked(MenuTopmost, _settings.AlwaysOnTop);
        SetMenuChecked(MenuMute, _settings.Muted);
        foreach (var scale in new[] { MenuScale75, MenuScale100, MenuScale125, MenuScale150 })
            SetMenuChecked(scale, _settings.ScalePercent == scale);
    }

    private void SetMenuChecked(int id, bool value)
    {
        var index = _trayMenu.GetItemIndex(id);
        if (index >= 0)
            _trayMenu.SetItemChecked(index, value);
    }

    private void ApplySettings(bool placeAtDefaultWhenUnset)
    {
        _window.AlwaysOnTop = _settings.AlwaysOnTop;
        _audio.Muted = _settings.Muted;
        ApplyScale();
        if (_settings.X >= 0 && _settings.Y >= 0)
            _window.Position = ClampToPrimaryWorkArea(new Vector2I(_settings.X, _settings.Y));
        else if (placeAtDefaultWhenUnset)
            PlaceAtLowerRight();
    }

    private void ApplyScale()
    {
        var factor = _settings.ScalePercent / 100f;
        _window.Size = new Vector2I(
            Mathf.RoundToInt(512 * factor),
            Mathf.RoundToInt(512 * factor));
        _window.Position = ClampToPrimaryWorkArea(_window.Position);
    }

    private void PlaceAtLowerRight()
    {
        var workArea = DisplayServer.ScreenGetUsableRect(DisplayServer.GetPrimaryScreen());
        if (workArea.Size.X < _window.Size.X || workArea.Size.Y < _window.Size.Y)
        {
            _window.Position = Vector2I.Zero;
            return;
        }
        _window.Position = new Vector2I(
            workArea.End.X - _window.Size.X - 20,
            workArea.End.Y - _window.Size.Y - 20);
    }

    private Vector2I ClampToPrimaryWorkArea(Vector2I proposed)
    {
        var workArea = DisplayServer.ScreenGetUsableRect(DisplayServer.GetPrimaryScreen());
        if (workArea.Size.X < _window.Size.X || workArea.Size.Y < _window.Size.Y)
            return proposed;
        return new Vector2I(
            Mathf.Clamp(proposed.X, workArea.Position.X, workArea.End.X - _window.Size.X),
            Mathf.Clamp(proposed.Y, workArea.Position.Y, workArea.End.Y - _window.Size.Y));
    }

    private void LoadSettings()
    {
        var path = ProjectSettings.GlobalizePath("user://settings.json");
        _settings = File.Exists(path)
            ? SettingsJson.Deserialize(File.ReadAllText(path))
            : PetSettings.Default;
    }

    private void SaveCurrentPosition() =>
        _settings = _settings with { X = _window.Position.X, Y = _window.Position.Y };

    private void SaveSettings()
    {
        var path = ProjectSettings.GlobalizePath("user://settings.json");
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, SettingsJson.Serialize(_settings));
    }

    private void SetMousePassthroughContour()
    {
        _window.MousePassthroughPolygon = new[]
        {
            new Vector2(80, 75),
            new Vector2(155, 18),
            new Vector2(330, 8),
            new Vector2(438, 70),
            new Vector2(505, 260),
            new Vector2(512, 512),
            new Vector2(0, 512),
            new Vector2(0, 420),
            new Vector2(55, 255)
        };
    }
}
