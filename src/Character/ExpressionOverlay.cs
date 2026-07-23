using Godot;
using System.Collections.Generic;
using System.Linq;

namespace DesktopPet.Character;

public partial class ExpressionOverlay : Node2D
{
    public enum EyeExpression
    {
        Open,
        Blink,
        Star,
        GreaterLess
    }

    private EyeExpression _expression;
    private MouthExpression _mouth;

    public enum MouthExpression
    {
        Original,
        Hungry,
        Maximum,
        Closed
    }

    public void SetExpression(EyeExpression expression)
    {
        _expression = expression;
        QueueRedraw();
    }

    public void SetMouth(MouthExpression expression)
    {
        _mouth = expression;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var skin = new Color("#f9d9df");
        if (_expression != EyeExpression.Open)
        {
            DrawEllipse(new Vector2(165, 299), new Vector2(49, 38), skin);
            DrawEllipse(new Vector2(319, 300), new Vector2(49, 38), skin);

            switch (_expression)
            {
                case EyeExpression.Blink:
                    DrawBlink(new Vector2(165, 302), false);
                    DrawBlink(new Vector2(319, 302), true);
                    break;
                case EyeExpression.Star:
                    DrawStar(new Vector2(165, 298), 28, 12, new Color("#78f5f0"));
                    DrawStar(new Vector2(319, 299), 28, 12, new Color("#b5ff83"));
                    break;
                case EyeExpression.GreaterLess:
                    DrawGreaterLess(new Vector2(165, 300), true);
                    DrawGreaterLess(new Vector2(319, 300), false);
                    break;
            }
        }

        if (_mouth != MouthExpression.Original)
        {
            DrawEllipse(new Vector2(242, 376), new Vector2(67, 54), skin);
            switch (_mouth)
            {
                case MouthExpression.Hungry:
                    DrawEllipse(new Vector2(242, 379), new Vector2(32, 27), new Color("#512f43"));
                    DrawEllipse(new Vector2(242, 392), new Vector2(20, 9), new Color("#f38ca4"));
                    break;
                case MouthExpression.Maximum:
                    DrawEllipse(new Vector2(242, 374), new Vector2(54, 51), new Color("#3a2233"));
                    DrawEllipse(new Vector2(242, 400), new Vector2(37, 16), new Color("#f0819e"));
                    break;
                case MouthExpression.Closed:
                    DrawPolyline(new[]
                    {
                        new Vector2(215, 378),
                        new Vector2(242, 385),
                        new Vector2(269, 378)
                    }, new Color("#6a394a"), 7, true);
                    break;
            }
        }
    }

    private void DrawBlink(Vector2 center, bool mirror)
    {
        var points = new List<Vector2>();
        for (var index = 0; index <= 12; index++)
        {
            var t = index / 12f;
            var x = Mathf.Lerp(-30, 30, t);
            var y = Mathf.Sin(t * Mathf.Pi) * (mirror ? 10 : 9);
            points.Add(center + new Vector2(x, y));
        }
        DrawPolyline(points.ToArray(), new Color("#5b3440"), 7, true);
    }

    private void DrawStar(Vector2 center, float outer, float inner, Color fill)
    {
        var points = new List<Vector2>();
        for (var index = 0; index < 8; index++)
        {
            var angle = -Mathf.Pi / 2 + index * Mathf.Pi / 4;
            var radius = index % 2 == 0 ? outer : inner;
            points.Add(center + Vector2.FromAngle(angle) * radius);
        }
        DrawColoredPolygon(points.ToArray(), fill);
        DrawPolyline(points.Concat(new[] { points[0] }).ToArray(),
            new Color("#6c4ca5"), 4, true);
    }

    private void DrawGreaterLess(Vector2 center, bool greater)
    {
        var direction = greater ? 1f : -1f;
        var points = new[]
        {
            center + new Vector2(-24 * direction, -22),
            center + new Vector2(20 * direction, 0),
            center + new Vector2(-24 * direction, 22)
        };
        DrawPolyline(points, new Color("#5b3440"), 9, true);
    }

    private void DrawEllipse(Vector2 center, Vector2 radii, Color color)
    {
        var points = new List<Vector2>();
        for (var index = 0; index < 40; index++)
        {
            var angle = index * Mathf.Tau / 40f;
            points.Add(center + new Vector2(Mathf.Cos(angle) * radii.X, Mathf.Sin(angle) * radii.Y));
        }
        DrawColoredPolygon(points.ToArray(), color);
    }
}
