using System.Numerics;

namespace DesktopPet.Core;

public static class CharacterAnimationMath
{
    public static Vector2 ClampEllipse(Vector2 value, Vector2 radii)
    {
        if (radii.X <= 0 || radii.Y <= 0)
            return Vector2.Zero;

        var normalizedX = value.X / radii.X;
        var normalizedY = value.Y / radii.Y;
        var lengthSquared = normalizedX * normalizedX + normalizedY * normalizedY;
        if (lengthSquared <= 1f)
            return value;

        var scale = 1f / MathF.Sqrt(lengthSquared);
        return new Vector2(value.X * scale, value.Y * scale);
    }
}
