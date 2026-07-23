using System.Numerics;

namespace DesktopPet.Core;

public static class EyeConstraint
{
    public static Vector2 Clamp(Vector2 desired, Vector2 radii)
    {
        if (radii.X <= 0 || radii.Y <= 0)
            return Vector2.Zero;

        var normalizedDistance =
            desired.X * desired.X / (radii.X * radii.X) +
            desired.Y * desired.Y / (radii.Y * radii.Y);

        return normalizedDistance <= 1f
            ? desired
            : desired / MathF.Sqrt(normalizedDistance);
    }
}
