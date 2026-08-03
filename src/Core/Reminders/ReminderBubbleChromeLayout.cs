using System.Collections.Generic;

namespace DesktopPet.Core.Reminders;

public readonly record struct ScreenPoint(int X, int Y);

public readonly record struct BubbleChromeLayout(
    ScreenRect Card,
    IReadOnlyList<ScreenPoint> Tail);

public static class ReminderBubbleChromeLayout
{
    public const int ChromeInset = 22;
    private const int CornerSafeDistance = 44;
    private const int TailHalfWidth = 13;
    private const int TailTipMargin = 7;

    public static BubbleChromeLayout Calculate(
        int width,
        int height,
        BubbleSide side,
        int tailX,
        int tailY)
    {
        if (width <= ChromeInset * 2 || height <= ChromeInset * 2)
            throw new ArgumentOutOfRangeException(nameof(width));

        var card = new ScreenRect(
            ChromeInset,
            ChromeInset,
            width - ChromeInset * 2,
            height - ChromeInset * 2);
        var anchorX = Math.Clamp(
            tailX,
            card.X + CornerSafeDistance,
            card.Right - CornerSafeDistance);
        var anchorY = Math.Clamp(
            tailY,
            card.Y + CornerSafeDistance,
            card.Bottom - CornerSafeDistance);

        ScreenPoint[] tail = side switch
        {
            BubbleSide.Above =>
            [
                new ScreenPoint(anchorX - TailHalfWidth, card.Bottom - 1),
                new ScreenPoint(anchorX + TailHalfWidth, card.Bottom - 1),
                new ScreenPoint(anchorX, height - TailTipMargin)
            ],
            BubbleSide.UpperRight =>
            [
                new ScreenPoint(card.X + 1, anchorY - TailHalfWidth),
                new ScreenPoint(card.X + 1, anchorY + TailHalfWidth),
                new ScreenPoint(TailTipMargin, anchorY)
            ],
            BubbleSide.UpperLeft =>
            [
                new ScreenPoint(card.Right - 1, anchorY - TailHalfWidth),
                new ScreenPoint(card.Right - 1, anchorY + TailHalfWidth),
                new ScreenPoint(width - TailTipMargin, anchorY)
            ],
            BubbleSide.Below =>
            [
                new ScreenPoint(anchorX - TailHalfWidth, card.Y + 1),
                new ScreenPoint(anchorX + TailHalfWidth, card.Y + 1),
                new ScreenPoint(anchorX, TailTipMargin)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(side))
        };
        return new BubbleChromeLayout(card, tail);
    }
}
