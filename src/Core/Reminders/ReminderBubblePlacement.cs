namespace DesktopPet.Core.Reminders;

public readonly record struct ScreenRect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(ScreenRect other) =>
        other.X >= X && other.Y >= Y &&
        other.Right <= Right && other.Bottom <= Bottom;
}

public enum BubbleSide
{
    Above,
    UpperRight,
    UpperLeft,
    Below
}

public readonly record struct BubblePlacement(
    ScreenRect Bounds,
    BubbleSide Side,
    int TailX,
    int TailY);

public static class ReminderBubblePlacement
{
    public static BubblePlacement Calculate(
        ScreenRect pet,
        int bubbleWidth,
        int bubbleHeight,
        ScreenRect workArea,
        int gap)
    {
        if (bubbleWidth <= 0 || bubbleHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(bubbleWidth));

        var centeredX = pet.X + (pet.Width - bubbleWidth) / 2;
        var candidates = new[]
        {
            (new ScreenRect(centeredX, pet.Y - gap - bubbleHeight, bubbleWidth, bubbleHeight), BubbleSide.Above),
            (new ScreenRect(pet.Right + gap, pet.Y, bubbleWidth, bubbleHeight), BubbleSide.UpperRight),
            (new ScreenRect(pet.X - gap - bubbleWidth, pet.Y, bubbleWidth, bubbleHeight), BubbleSide.UpperLeft),
            (new ScreenRect(centeredX, pet.Bottom + gap, bubbleWidth, bubbleHeight), BubbleSide.Below)
        };

        foreach (var candidate in candidates)
        {
            if (workArea.Contains(candidate.Item1))
                return CreateResult(candidate.Item1, candidate.Item2, pet);
        }

        var preferred = pet.Y >= workArea.Y + bubbleHeight + gap
            ? candidates[0]
            : candidates[3];
        var clamped = Clamp(preferred.Item1, workArea);
        return CreateResult(clamped, preferred.Item2, pet);
    }

    private static BubblePlacement CreateResult(
        ScreenRect bounds,
        BubbleSide side,
        ScreenRect pet)
    {
        var petCenter = pet.X + pet.Width / 2;
        var petMiddle = pet.Y + pet.Height / 2;
        var tailX = Math.Clamp(petCenter - bounds.X, 24, Math.Max(24, bounds.Width - 24));
        var tailY = Math.Clamp(petMiddle - bounds.Y, 44, Math.Max(44, bounds.Height - 44));
        return new BubblePlacement(bounds, side, tailX, tailY);
    }

    private static ScreenRect Clamp(ScreenRect value, ScreenRect workArea)
    {
        var width = Math.Min(value.Width, workArea.Width);
        var height = Math.Min(value.Height, workArea.Height);
        var x = Math.Clamp(value.X, workArea.X, workArea.Right - width);
        var y = Math.Clamp(value.Y, workArea.Y, workArea.Bottom - height);
        return new ScreenRect(x, y, width, height);
    }
}
