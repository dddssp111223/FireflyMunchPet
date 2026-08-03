using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopPet.Character;

public partial class CharacterRig : Node2D
{
    private enum BodyRole
    {
        Idle,
        Hover,
        Blink,
        Anticipation,
        Maximum,
        Gulp
    }

    private sealed record TextureBank(
        Texture2D Idle,
        Texture2D Hover,
        Texture2D Blink,
        Texture2D Anticipation,
        Texture2D Maximum,
        Texture2D Gulp,
        Texture2D Desk);

    [Signal]
    public delegate void OneShotFinishedEventHandler();

    [Signal]
    public delegate void ReminderBouncePulseEventHandler();

    private Node2D _characterRoot = null!;
    private Sprite2D _body = null!;
    private Sprite2D _bodyBlend = null!;
    private Sprite2D _desk = null!;
    private EyeRig _eyeRig = null!;
    private TextureBank _originalBank = null!;
    private TextureBank _harmonizedBank = null!;
    private TextureBank _activeBank = null!;
    private BodyRole _bodyRole = BodyRole.Idle;
    private Vector2 _characterRestPosition;
    private float _phase;
    private float _rejectOffsetX;
    private double _blinkCountdown = 3.2;
    private bool _eyeTracking = true;
    private bool _idleMotionEnabled = true;

    public override void _Ready()
    {
        _characterRoot = GetNode<Node2D>("CharacterMotionRoot");
        _body = GetNode<Sprite2D>("CharacterMotionRoot/Body");
        _bodyBlend = GetNode<Sprite2D>("CharacterMotionRoot/BodyBlend");
        _desk = GetNode<Sprite2D>("StaticDesk");
        _eyeRig = GetNode<EyeRig>("CharacterMotionRoot/EyeRig");
        _characterRestPosition = _characterRoot.Position;

        _originalBank = LoadTextureBank("res://assets/character/layers");
        _harmonizedBank = LoadTextureBank("res://assets/character/layers_harmonized");
        _activeBank = _originalBank;
        ApplyActiveBank();
        _eyeRig.SetMode(EyeRig.EyeMode.Normal);
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        _characterRoot.Position =
            _characterRestPosition + new Vector2(_rejectOffsetX, 0f);
        _characterRoot.Rotation = _idleMotionEnabled
            ? Mathf.Sin(_phase * 0.72f) * 0.0025f
            : 0f;

        if (_eyeTracking)
            _eyeRig.SetTarget(_characterRoot.ToLocal(GetGlobalMousePosition()));

        _blinkCountdown -= delta;
        if (_blinkCountdown <= 0 && _eyeTracking)
        {
            _blinkCountdown = GD.RandRange(2.5, 6.5);
            _ = PlayBlinkAsync();
        }
    }

    public void SetFileHover(bool active)
    {
        _eyeTracking = !active;
        SetBodyRole(active ? BodyRole.Hover : BodyRole.Idle);
        _bodyBlend.Modulate = new Color(1, 1, 1, 0);
        _eyeRig.SetMode(active ? EyeRig.EyeMode.Star : EyeRig.EyeMode.Normal);

        var tween = CreateTween();
        tween.TweenProperty(
                _characterRoot,
                "scale",
                active ? new Vector2(1.025f, 1.025f) : Vector2.One,
                0.14)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public async void PlayClickBounce()
    {
        await PlayBounceTweenAsync();
        EmitSignal(SignalName.OneShotFinished);
    }

    public async void PlayReminderBounceSequence()
    {
        EmitSignal(SignalName.ReminderBouncePulse);
        var first = PlayBounceTweenAsync();
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
        await first;
        EmitSignal(SignalName.ReminderBouncePulse);
        await PlayBounceTweenAsync();
        EmitSignal(SignalName.OneShotFinished);
    }

    private async Task PlayBounceTweenAsync()
    {
        var tween = CreateTween();
        tween.TweenProperty(_characterRoot, "scale", new Vector2(1.08f, 0.86f), 0.07)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_characterRoot, "scale", new Vector2(0.96f, 1.08f), 0.095)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_characterRoot, "scale", Vector2.One, 0.145)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public void SetEyeTrackingEnabled(bool enabled)
    {
        _eyeTracking = enabled;
        if (enabled)
            _eyeRig.SetMode(EyeRig.EyeMode.Normal);
    }

    public void SetIdleMotionEnabled(bool enabled) => _idleMotionEnabled = enabled;

    public void SetHarmonizedMode(bool enabled)
    {
        _activeBank = enabled ? _harmonizedBank : _originalBank;
        ApplyActiveBank();
        _eyeRig.SetHarmonizedMode(enabled);
    }

    public async void PlaySwallow(IReadOnlyList<Texture2D> icons, Vector2 dropPoint)
    {
        _eyeTracking = false;
        _eyeRig.SetMode(EyeRig.EyeMode.Hidden);
        _characterRoot.Scale = Vector2.One;
        await CrossFadeTo(BodyRole.Anticipation, 0.06);

        var iconLayer = new Node2D
        {
            Position = dropPoint,
            ZIndex = 100
        };
        AddChild(iconLayer);
        for (var index = 0; index < icons.Count && index < 3; index++)
        {
            var iconSprite = new Sprite2D
            {
                Texture = icons[index],
                Position = new Vector2(index * 8, -index * 7),
                Scale = Vector2.One * 1.55f
            };
            iconLayer.AddChild(iconSprite);
        }

        await ToSignal(GetTree().CreateTimer(0.06), SceneTreeTimer.SignalName.Timeout);
        await CrossFadeTo(BodyRole.Maximum, 0.065);

        var feedTween = CreateTween().SetParallel();
        feedTween.TweenProperty(iconLayer, "position", new Vector2(242, 376), 0.24)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        feedTween.TweenProperty(iconLayer, "scale", Vector2.One * 0.22f, 0.24)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        feedTween.TweenProperty(iconLayer, "rotation", 0.22f, 0.24)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        await ToSignal(feedTween, Tween.SignalName.Finished);
        iconLayer.QueueFree();

        await CrossFadeTo(BodyRole.Gulp, 0.055);
        var gulpTween = CreateTween();
        gulpTween.TweenProperty(_characterRoot, "scale", new Vector2(1.055f, 0.82f), 0.15)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        gulpTween.TweenProperty(_characterRoot, "scale", new Vector2(0.97f, 1.085f), 0.15)
            .SetTrans(Tween.TransitionType.Back)
            .SetEase(Tween.EaseType.Out);
        gulpTween.TweenProperty(_characterRoot, "scale", Vector2.One, 0.24)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(gulpTween, Tween.SignalName.Finished);

        await CrossFadeTo(BodyRole.Idle, 0.075);
        _eyeRig.SetMode(EyeRig.EyeMode.Normal);
        _eyeTracking = true;
        EmitSignal(SignalName.OneShotFinished);
    }

    public async void PlayReject()
    {
        var tween = CreateTween();
        tween.TweenMethod(
            Callable.From<float>(value => _rejectOffsetX = value),
            0f, -7f, 0.055);
        tween.TweenMethod(
            Callable.From<float>(value => _rejectOffsetX = value),
            -7f, 7f, 0.07);
        tween.TweenMethod(
            Callable.From<float>(value => _rejectOffsetX = value),
            7f, 0f, 0.07);
        await ToSignal(tween, Tween.SignalName.Finished);
        _rejectOffsetX = 0;
        EmitSignal(SignalName.OneShotFinished);
    }

    private async Task PlayBlinkAsync()
    {
        _eyeTracking = false;
        _eyeRig.SetMode(EyeRig.EyeMode.Hidden);
        await CrossFadeTo(BodyRole.Blink, 0.035);
        await ToSignal(GetTree().CreateTimer(0.065), SceneTreeTimer.SignalName.Timeout);
        await CrossFadeTo(BodyRole.Idle, 0.035);
        _eyeRig.SetMode(EyeRig.EyeMode.Normal);
        _eyeTracking = true;
    }

    private async Task CrossFadeTo(BodyRole role, double duration)
    {
        _bodyRole = role;
        _bodyBlend.Texture = ResolveBody(role);
        _bodyBlend.Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.TweenProperty(_bodyBlend, "modulate:a", 1f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
        await ToSignal(tween, Tween.SignalName.Finished);
        _body.Texture = ResolveBody(role);
        _bodyBlend.Modulate = new Color(1, 1, 1, 0);
    }

    private static TextureBank LoadTextureBank(string directory) => new(
        GD.Load<Texture2D>($"{directory}/body_idle.png"),
        GD.Load<Texture2D>($"{directory}/body_hover.png"),
        GD.Load<Texture2D>($"{directory}/body_blink.png"),
        GD.Load<Texture2D>($"{directory}/body_swallow_anticipation.png"),
        GD.Load<Texture2D>($"{directory}/body_swallow_max.png"),
        GD.Load<Texture2D>($"{directory}/body_swallow_gulp.png"),
        GD.Load<Texture2D>($"{directory}/desk.png"));

    private Texture2D ResolveBody(BodyRole role) => role switch
    {
        BodyRole.Idle => _activeBank.Idle,
        BodyRole.Hover => _activeBank.Hover,
        BodyRole.Blink => _activeBank.Blink,
        BodyRole.Anticipation => _activeBank.Anticipation,
        BodyRole.Maximum => _activeBank.Maximum,
        BodyRole.Gulp => _activeBank.Gulp,
        _ => _activeBank.Idle
    };

    private void SetBodyRole(BodyRole role)
    {
        _bodyRole = role;
        _body.Texture = ResolveBody(role);
    }

    private void ApplyActiveBank()
    {
        _body.Texture = ResolveBody(_bodyRole);
        _bodyBlend.Texture = _body.Texture;
        _bodyBlend.Modulate = new Color(1, 1, 1, 0);
        _desk.Texture = _activeBank.Desk;
    }
}
