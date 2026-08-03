using DesktopPet.Core.Reminders;
using Godot;
using System;

namespace DesktopPet.App.Reminders;

public partial class ReminderBubbleView : Control
{
    public event Action? Acknowledged;

    private PanelContainer _card = null!;
    private Label _body = null!;
    private BubbleSide _side = BubbleSide.Above;
    private int _tailX = 224;
    private int _tailY = 119;

    public override void _Ready()
    {
        Theme = ReminderTheme.CreateTheme();
        MouseFilter = MouseFilterEnum.Stop;

        _card = new PanelContainer();
        var cloudStyle = ReminderTheme.RoundedBox(
            ReminderTheme.MintSoft,
            ReminderTheme.Line,
            26);
        cloudStyle.ContentMarginLeft = 0;
        cloudStyle.ContentMarginTop = 0;
        cloudStyle.ContentMarginRight = 0;
        cloudStyle.ContentMarginBottom = 0;
        cloudStyle.ShadowColor = new Color(0.08f, 0.28f, 0.24f, 0.18f);
        cloudStyle.ShadowSize = 8;
        cloudStyle.ShadowOffset = new Vector2(0, 4);
        _card.AddThemeStyleboxOverride("panel", cloudStyle);
        AddChild(_card);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        _card.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 9);
        margin.AddChild(root);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 7);
        var sparkle = new Label { Text = "✦♡" };
        sparkle.AddThemeColorOverride("font_color", ReminderTheme.Mint);
        header.AddChild(sparkle);
        var title = new Label
        {
            Text = "提醒",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", ReminderTheme.MintDeep);
        title.AddThemeFontSizeOverride("font_size", 15);
        header.AddChild(title);
        var now = new Label { Text = "现在" };
        now.AddThemeColorOverride("font_color", ReminderTheme.Muted);
        header.AddChild(now);
        root.AddChild(header);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(0, 82),
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _body = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Top
        };
        _body.AddThemeFontSizeOverride("font_size", 15);
        scroll.AddChild(_body);
        root.AddChild(scroll);

        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        var acknowledge = new Button
        {
            Text = "知道了",
            CustomMinimumSize = new Vector2(88, 38)
        };
        ReminderTheme.StylePrimary(acknowledge);
        acknowledge.Pressed += () => Acknowledged?.Invoke();
        actions.AddChild(acknowledge);
        root.AddChild(actions);

        UpdateChrome();
    }

    public override void _Draw()
    {
        if (Size.X <= ReminderBubbleChromeLayout.ChromeInset * 2 ||
            Size.Y <= ReminderBubbleChromeLayout.ChromeInset * 2)
            return;

        var layout = ReminderBubbleChromeLayout.Calculate(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y),
            _side,
            _tailX,
            _tailY);
        var points = new Vector2[layout.Tail.Count];
        for (var index = 0; index < layout.Tail.Count; index++)
            points[index] = new Vector2(layout.Tail[index].X, layout.Tail[index].Y);
        DrawColoredPolygon(points, ReminderTheme.MintSoft);
        DrawPolyline(
            new[] { points[0], points[1], points[2], points[0] },
            ReminderTheme.Line,
            1.5f,
            true);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized && IsInstanceValid(_card))
            UpdateChrome();
    }

    public void SetText(string text)
    {
        if (IsNodeReady())
            _body.Text = text;
        else
            Ready += () => _body.Text = text;
    }

    public void SetPlacement(BubblePlacement placement)
    {
        _side = placement.Side;
        _tailX = placement.TailX;
        _tailY = placement.TailY;
        if (IsNodeReady())
            UpdateChrome();
    }

    private void UpdateChrome()
    {
        if (!IsInstanceValid(_card) ||
            Size.X <= ReminderBubbleChromeLayout.ChromeInset * 2 ||
            Size.Y <= ReminderBubbleChromeLayout.ChromeInset * 2)
            return;

        var layout = ReminderBubbleChromeLayout.Calculate(
            Mathf.RoundToInt(Size.X),
            Mathf.RoundToInt(Size.Y),
            _side,
            _tailX,
            _tailY);
        _card.Position = new Vector2(layout.Card.X, layout.Card.Y);
        _card.Size = new Vector2(layout.Card.Width, layout.Card.Height);
        QueueRedraw();
    }
}
