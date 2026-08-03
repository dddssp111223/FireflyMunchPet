using System.Numerics;
using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class CharacterAnimationMathTests
{
    internal static void Run()
    {
        AssertEx.Equal(
            new Vector2(3, 2),
            CharacterAnimationMath.ClampEllipse(new Vector2(3, 2), new Vector2(7, 5)),
            "eye movement inside the authored ellipse remains unchanged");

        var clamped = CharacterAnimationMath.ClampEllipse(
            new Vector2(14, 10),
            new Vector2(7, 5));
        AssertEx.True(
            Math.Abs(clamped.X - 4.9497f) < 0.001f &&
            Math.Abs(clamped.Y - 3.5355f) < 0.001f,
            "diagonal eye movement clamps to the authored ellipse");

        AssertEx.Equal(
            Vector2.Zero,
            CharacterAnimationMath.ClampEllipse(new Vector2(3, 2), Vector2.Zero),
            "invalid eye radii disable movement safely");
    }
}
