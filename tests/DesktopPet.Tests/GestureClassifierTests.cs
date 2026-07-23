using System.Numerics;
using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class GestureClassifierTests
{
    public static void Run()
    {
        AssertEx.Equal(
            GestureKind.Click,
            GestureClassifier.Classify(HitRegion.Cheek, Vector2.Zero, new Vector2(2, 1), 8),
            "short cheek press is click");
        AssertEx.Equal(
            GestureKind.CheekDrag,
            GestureClassifier.Classify(HitRegion.Cheek, Vector2.Zero, new Vector2(12, 0), 8),
            "cheek movement becomes pull");
        AssertEx.Equal(
            GestureKind.WindowDrag,
            GestureClassifier.Classify(HitRegion.MoveHandle, Vector2.Zero, new Vector2(0, 12), 8),
            "hair movement becomes window drag");
        AssertEx.Equal(
            GestureKind.None,
            GestureClassifier.Classify(HitRegion.Visible, Vector2.Zero, new Vector2(12, 0), 8),
            "large movement on ordinary pixels is not a click");
    }
}
