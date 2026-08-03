using DesktopPet.Core;
using Godot;
using NumericsVector2 = System.Numerics.Vector2;

namespace DesktopPet.Character;

public partial class EyeRig : Node2D
{
    public enum EyeMode
    {
        Normal,
        Star,
        Hidden
    }

    private Sprite2D _leftNormal = null!;
    private Sprite2D _rightNormal = null!;
    private Sprite2D _leftStar = null!;
    private Sprite2D _rightStar = null!;
    private Sprite2D _leftClip = null!;
    private Sprite2D _rightClip = null!;
    private Vector2 _target;
    private Vector2 _leftOffset;
    private Vector2 _rightOffset;
    private EyeMode _mode = EyeMode.Normal;

    public override void _Ready()
    {
        _leftClip = GetNode<Sprite2D>("LeftEyeClip");
        _rightClip = GetNode<Sprite2D>("RightEyeClip");
        _leftNormal = GetNode<Sprite2D>("LeftEyeClip/Normal");
        _rightNormal = GetNode<Sprite2D>("RightEyeClip/Normal");
        _leftStar = GetNode<Sprite2D>("LeftEyeClip/Star");
        _rightStar = GetNode<Sprite2D>("RightEyeClip/Star");
        SetHarmonizedMode(false);
        ApplyMode();
    }

    public override void _Process(double delta)
    {
        if (_mode == EyeMode.Normal)
        {
            _leftOffset = SmoothOffset(
                _leftOffset,
                DesiredOffset(_target - _leftClip.Position),
                delta);
            _rightOffset = SmoothOffset(
                _rightOffset,
                DesiredOffset(_target - _rightClip.Position),
                delta);
            _leftNormal.Position = _leftOffset;
            _rightNormal.Position = _rightOffset;
        }
    }

    public void SetTarget(Vector2 localTarget) => _target = localTarget;

    public void SetHarmonizedMode(bool enabled)
    {
        var directory = enabled
            ? "res://assets/character/layers_harmonized"
            : "res://assets/character/layers";
        _leftClip.Texture = GD.Load<Texture2D>($"{directory}/left_eye_mask.png");
        _rightClip.Texture = GD.Load<Texture2D>($"{directory}/right_eye_mask.png");
        _leftNormal.Texture = GD.Load<Texture2D>($"{directory}/left_iris_idle.png");
        _rightNormal.Texture = GD.Load<Texture2D>($"{directory}/right_iris_idle.png");
        _leftStar.Texture = GD.Load<Texture2D>($"{directory}/left_iris_star.png");
        _rightStar.Texture = GD.Load<Texture2D>($"{directory}/right_iris_star.png");
        _leftStar.Scale = Vector2.One;
        _rightStar.Scale = Vector2.One;
        ApplyMode();
    }

    public void SetMode(EyeMode mode)
    {
        _mode = mode;
        if (mode != EyeMode.Normal)
        {
            _leftOffset = Vector2.Zero;
            _rightOffset = Vector2.Zero;
            _leftNormal.Position = Vector2.Zero;
            _rightNormal.Position = Vector2.Zero;
        }
        ApplyMode();
    }

    private static Vector2 DesiredOffset(Vector2 delta)
    {
        var requested = delta / 46f;
        var clamped = CharacterAnimationMath.ClampEllipse(
            new NumericsVector2(requested.X, requested.Y),
            new NumericsVector2(4f, 3f));
        return new Vector2(clamped.X, clamped.Y);
    }

    private static Vector2 SmoothOffset(Vector2 current, Vector2 target, double delta)
    {
        var weight = 1f - Mathf.Exp(-18f * (float)delta);
        return current.Lerp(target, weight);
    }

    private void ApplyMode()
    {
        _leftNormal.Visible = _mode == EyeMode.Normal;
        _rightNormal.Visible = _mode == EyeMode.Normal;
        _leftStar.Visible = _mode == EyeMode.Star;
        _rightStar.Visible = _mode == EyeMode.Star;
    }
}
