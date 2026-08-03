using System.Numerics;

namespace DesktopPet.Core;

public enum HitRegion
{
    Visible,
    MoveHandle
}

public enum GestureKind
{
    None,
    Click,
    WindowDrag
}

public static class GestureClassifier
{
    public static GestureKind Classify(
        HitRegion region,
        Vector2 down,
        Vector2 current,
        float threshold)
    {
        if (Vector2.Distance(down, current) < threshold)
            return GestureKind.Click;

        return region switch
        {
            HitRegion.MoveHandle => GestureKind.WindowDrag,
            _ => GestureKind.None
        };
    }
}
