using DesktopPet.Core;
using Godot;
using System.Threading.Tasks;
using NumericsVector2 = System.Numerics.Vector2;

namespace DesktopPet.Character;

public partial class CharacterRig : Node2D
{
    [Signal]
    public delegate void OneShotFinishedEventHandler();

    private Sprite2D _sprite = null!;
    private ExpressionOverlay _overlay = null!;
    private ShaderMaterial _material = null!;
    private float _phase;
    private double _blinkCountdown = 3.2;
    private bool _eyeTracking = true;

    public override void _Ready()
    {
        _sprite = GetNode<Sprite2D>("Sprite");
        _overlay = GetNode<ExpressionOverlay>("ExpressionOverlay");
        _material = (ShaderMaterial)_sprite.Material;
    }

    public override void _Process(double delta)
    {
        _phase += (float)delta;
        _material.SetShaderParameter("idle_phase", _phase);

        Position = new Vector2(0, Mathf.Sin(_phase * 1.35f) * 1.8f);
        Rotation = Mathf.Sin(_phase * 0.72f) * 0.0025f;

        if (_eyeTracking)
            UpdateEyeTarget();

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
        _overlay.SetExpression(active
            ? ExpressionOverlay.EyeExpression.Star
            : ExpressionOverlay.EyeExpression.Open);
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", active ? new Vector2(1.025f, 1.025f) : Vector2.One, 0.12);
    }

    public async void PlayClickBounce()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector2(1.08f, 0.86f), 0.07)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "scale", new Vector2(0.96f, 1.08f), 0.095)
            .SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "scale", Vector2.One, 0.145)
            .SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(tween, Tween.SignalName.Finished);
        EmitSignal(SignalName.OneShotFinished);
    }

    public void SetCheekPull(Vector2 displacement)
    {
        var limited = displacement.LimitLength(72) / 512f;
        _material.SetShaderParameter("cheek_pull", limited);
    }

    public void ReleaseCheek()
    {
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<Vector2>(value =>
            _material.SetShaderParameter("cheek_pull", value)),
            (Vector2)_material.GetShaderParameter("cheek_pull"),
            Vector2.Zero,
            0.22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    public async void PlaySwallow()
    {
        _eyeTracking = false;
        _overlay.SetExpression(ExpressionOverlay.EyeExpression.GreaterLess);
        var tween = CreateTween();
        tween.TweenMethod(Callable.From<float>(value =>
            _material.SetShaderParameter("swallow", value)), 0f, 1f, 0.18);
        tween.TweenProperty(this, "scale", new Vector2(1.06f, 0.80f), 0.18);
        tween.TweenProperty(this, "scale", new Vector2(0.96f, 1.10f), 0.14)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "scale", Vector2.One, 0.24);
        await ToSignal(tween, Tween.SignalName.Finished);
        _material.SetShaderParameter("swallow", 0f);
        _overlay.SetExpression(ExpressionOverlay.EyeExpression.Open);
        _eyeTracking = true;
        EmitSignal(SignalName.OneShotFinished);
    }

    public async void PlayReject()
    {
        var original = Position;
        var tween = CreateTween();
        tween.TweenProperty(this, "position:x", original.X - 7, 0.055);
        tween.TweenProperty(this, "position:x", original.X + 7, 0.07);
        tween.TweenProperty(this, "position:x", original.X, 0.07);
        await ToSignal(tween, Tween.SignalName.Finished);
        EmitSignal(SignalName.OneShotFinished);
    }

    private async Task PlayBlinkAsync()
    {
        _eyeTracking = false;
        _overlay.SetExpression(ExpressionOverlay.EyeExpression.Blink);
        await ToSignal(GetTree().CreateTimer(0.11), SceneTreeTimer.SignalName.Timeout);
        _overlay.SetExpression(ExpressionOverlay.EyeExpression.Open);
        _eyeTracking = true;
    }

    private void UpdateEyeTarget()
    {
        var desired = (GetGlobalMousePosition() - new Vector2(242, 299)) / 42f;
        var limited = EyeConstraint.Clamp(
            new NumericsVector2(desired.X, desired.Y),
            new NumericsVector2(7, 5));
        _material.SetShaderParameter("eye_offset",
            new Vector2(limited.X / 512f, limited.Y / 512f));
    }
}
