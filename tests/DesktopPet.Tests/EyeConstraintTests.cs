using System.Numerics;
using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class EyeConstraintTests
{
    public static void Run()
    {
        var clamped = EyeConstraint.Clamp(new Vector2(20, 20), new Vector2(7, 5));
        var ellipse = clamped.X * clamped.X / 49f + clamped.Y * clamped.Y / 25f;

        AssertEx.True(ellipse <= 1.0001f, "iris remains inside ellipse");
        AssertEx.Equal(
            Vector2.Zero,
            EyeConstraint.Clamp(Vector2.Zero, new Vector2(7, 5)),
            "center stays centered");
        AssertEx.Equal(
            Vector2.Zero,
            EyeConstraint.Clamp(new Vector2(4, 4), Vector2.Zero),
            "invalid radii center the iris");
    }
}
