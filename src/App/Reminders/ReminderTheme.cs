using Godot;

namespace DesktopPet.App.Reminders;

internal static class ReminderTheme
{
    internal static readonly Color Mint = new Color("63cbb4");
    internal static readonly Color MintDeep = new Color("369b84");
    internal static readonly Color MintSoft = new Color("e9faf5");
    internal static readonly Color Surface = new Color("f7fffc");
    internal static readonly Color Ink = new Color("24423d");
    internal static readonly Color Muted = new Color("66817a");
    internal static readonly Color Line = new Color("bfe9de");
    internal static readonly Color Peach = new Color("ffb3a7");
    internal static readonly Color Danger = new Color("d96161");

    internal static Theme CreateTheme()
    {
        var theme = new Theme { DefaultFontSize = 14 };
        theme.SetColor("font_color", "Label", Ink);
        ConfigureButton(theme, "Button");
        ConfigureButton(theme, "OptionButton");
        ConfigureToggle(theme, "CheckButton");
        ConfigureTextControl(theme, "LineEdit");
        ConfigureTextControl(theme, "TextEdit");
        return theme;
    }

    private static void ConfigureButton(Theme theme, string type)
    {
        theme.SetColor("font_color", type, Ink);
        theme.SetColor("font_hover_color", type, Ink);
        theme.SetColor("font_pressed_color", type, Ink);
        theme.SetColor("font_focus_color", type, Ink);
        theme.SetColor("font_disabled_color", type, Muted.Lightened(0.22f));
        theme.SetStylebox("normal", type, RoundedBox(Colors.White, Line, 9, 8, 7));
        theme.SetStylebox("hover", type, RoundedBox(MintSoft, Mint, 9, 8, 7));
        theme.SetStylebox("pressed", type, RoundedBox(MintSoft, MintDeep, 9, 8, 7));
        theme.SetStylebox("focus", type, RoundedBox(Colors.Transparent, Mint, 9, 8, 7));
        theme.SetStylebox("disabled", type, RoundedBox(MintSoft.Lightened(0.35f), Line, 9, 8, 7));
    }

    private static void ConfigureToggle(Theme theme, string type)
    {
        theme.SetColor("font_color", type, Ink);
        theme.SetColor("font_hover_color", type, Ink);
        theme.SetColor("font_pressed_color", type, Ink);
        theme.SetColor("font_hover_pressed_color", type, Ink);
        theme.SetColor("font_focus_color", type, Ink);
        theme.SetColor("font_disabled_color", type, Muted.Lightened(0.22f));
    }

    private static void ConfigureTextControl(Theme theme, string type)
    {
        theme.SetColor("font_color", type, Ink);
        theme.SetColor("font_placeholder_color", type, Muted);
        theme.SetColor("caret_color", type, MintDeep);
        theme.SetColor("selection_color", type, Mint.Lightened(0.35f));
        theme.SetStylebox("normal", type, RoundedBox(Colors.White, Line, 9, 10, 8));
        theme.SetStylebox("focus", type, RoundedBox(MintSoft.Lightened(0.45f), Mint, 9, 10, 8));
        theme.SetStylebox("read_only", type, RoundedBox(MintSoft.Lightened(0.25f), Line, 9, 10, 8));
    }

    internal static StyleBoxFlat RoundedBox(Color background, Color border, int radius)
        => RoundedBox(background, border, radius, 12, 10);

    internal static StyleBoxFlat RoundedBox(
        Color background,
        Color border,
        int radius,
        int horizontalMargin,
        int verticalMargin)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = horizontalMargin,
            ContentMarginTop = verticalMargin,
            ContentMarginRight = horizontalMargin,
            ContentMarginBottom = verticalMargin
        };
    }

    internal static void StylePrimary(Button button)
    {
        button.AddThemeColorOverride("font_color", Colors.White);
        button.AddThemeStyleboxOverride("normal", RoundedBox(MintDeep, MintDeep, 11));
        button.AddThemeStyleboxOverride("hover", RoundedBox(Mint, MintDeep, 11));
        button.AddThemeStyleboxOverride("pressed", RoundedBox(MintDeep.Darkened(0.12f), MintDeep, 11));
    }

    internal static void StyleSecondary(Button button)
    {
        button.AddThemeColorOverride("font_color", Ink);
        button.AddThemeStyleboxOverride("normal", RoundedBox(Surface, Line, 11));
        button.AddThemeStyleboxOverride("hover", RoundedBox(MintSoft, Mint, 11));
        button.AddThemeStyleboxOverride("pressed", RoundedBox(MintSoft.Darkened(0.04f), MintDeep, 11));
    }
}
