using DesktopPet.Core.Reminders;

namespace DesktopPet.Tests;

internal static class ReminderPresentationTests
{
    public static void Run()
    {
        QueueUsesStableOrderAndSupportsRemoval();
        PlacementPrefersAboveAndStaysVisible();
        CloudChromeKeepsTailAndCardInsideWindow();
    }

    private static void QueueUsesStableOrderAndSupportsRemoval()
    {
        var queue = new ReminderQueue();
        var first = new DueReminder(Guid.NewGuid(), DateTimeOffset.UnixEpoch, 1, false);
        var second = new DueReminder(Guid.NewGuid(), DateTimeOffset.UnixEpoch, 2, false);
        queue.Enqueue(new[] { second, first, first });
        AssertEx.Equal(2, queue.Count, "queue de-duplicates reminder ids");
        AssertEx.Equal(first.Id, queue.Current!.Id, "same-time reminders use list order");
        queue.AcknowledgeCurrent();
        AssertEx.Equal(second.Id, queue.Current!.Id, "acknowledgement advances queue");
        queue.Remove(second.Id);
        AssertEx.Equal<DueReminder?>(null, queue.Current, "deleted queued item removed");
        queue.Enqueue(new[] { first, second });
        queue.Clear();
        AssertEx.Equal(0, queue.Count, "queue clears when reminders are disabled");
    }

    private static void PlacementPrefersAboveAndStaysVisible()
    {
        var work = new ScreenRect(0, 0, 1920, 1040);
        var pet = new ScreenRect(1500, 600, 256, 256);
        var result = ReminderBubblePlacement.Calculate(pet, 420, 190, work, 12);
        AssertEx.True(work.Contains(result.Bounds), "bubble stays inside work area");
        AssertEx.Equal(BubbleSide.Above, result.Side, "bubble prefers above");
        AssertEx.True(result.TailX >= 24 && result.TailX <= 396, "tail avoids rounded corners");

        var topPet = new ScreenRect(0, 0, 154, 154);
        var topResult = ReminderBubblePlacement.Calculate(topPet, 420, 190, work, 12);
        AssertEx.True(work.Contains(topResult.Bounds), "top-edge fallback stays visible");
        AssertEx.True(topResult.Side != BubbleSide.Above, "top-edge uses fallback");

        var rightPet = new ScreenRect(1850, 500, 154, 154);
        var rightResult = ReminderBubblePlacement.Calculate(rightPet, 420, 190, work, 12);
        AssertEx.True(work.Contains(rightResult.Bounds), "right-edge bubble is clamped");

        var leftPet = new ScreenRect(0, 500, 154, 154);
        var leftResult = ReminderBubblePlacement.Calculate(leftPet, 420, 190, work, 12);
        AssertEx.True(work.Contains(leftResult.Bounds), "left-edge bubble stays visible");

        var bottomPet = new ScreenRect(900, 980, 154, 154);
        var bottomResult = ReminderBubblePlacement.Calculate(bottomPet, 420, 190, work, 12);
        AssertEx.True(work.Contains(bottomResult.Bounds), "bottom-edge bubble stays visible");

        var tinyWork = new ScreenRect(100, 100, 320, 160);
        var tinyResult = ReminderBubblePlacement.Calculate(
            new ScreenRect(180, 120, 100, 100), 420, 190, tinyWork, 12);
        AssertEx.True(tinyWork.Contains(tinyResult.Bounds), "oversized bubble is fitted to work area");
        AssertEx.True(tinyResult.Bounds.Width <= tinyWork.Width, "bubble width fits small work area");
        AssertEx.True(tinyResult.Bounds.Height <= tinyWork.Height, "bubble height fits small work area");
        AssertEx.True(rightResult.TailY >= 44, "side tail avoids top rounded corner");
        AssertEx.True(
            rightResult.TailY <= rightResult.Bounds.Height - 44,
            "side tail avoids bottom rounded corner");
    }

    private static void CloudChromeKeepsTailAndCardInsideWindow()
    {
        var window = new ScreenRect(0, 0, 448, 238);
        foreach (var side in Enum.GetValues<BubbleSide>())
        {
            var layout = ReminderBubbleChromeLayout.Calculate(448, 238, side, 224, 119);
            AssertEx.True(window.Contains(layout.Card), $"{side} card stays inside window");
            foreach (var point in layout.Tail)
            {
                AssertEx.True(
                    point.X >= 0 && point.X <= window.Right &&
                    point.Y >= 0 && point.Y <= window.Bottom,
                    $"{side} tail point stays inside window");
            }

            var tip = layout.Tail[2];
            switch (side)
            {
                case BubbleSide.Above:
                    AssertEx.True(tip.Y > layout.Card.Bottom, "above bubble tail points down");
                    break;
                case BubbleSide.UpperRight:
                    AssertEx.True(tip.X < layout.Card.X, "right bubble tail points left");
                    break;
                case BubbleSide.UpperLeft:
                    AssertEx.True(tip.X > layout.Card.Right, "left bubble tail points right");
                    break;
                case BubbleSide.Below:
                    AssertEx.True(tip.Y < layout.Card.Y, "below bubble tail points up");
                    break;
            }
        }
    }
}
