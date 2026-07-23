using DesktopPet.Core;

namespace DesktopPet.Tests;

internal static class SettingsJsonTests
{
    public static void Run()
    {
        var defaults = PetSettings.Default;
        var restored = SettingsJson.Deserialize(SettingsJson.Serialize(defaults));

        AssertEx.Equal(defaults, restored, "settings round trip");
        AssertEx.Equal(defaults, SettingsJson.Deserialize("{broken"), "corrupt settings fallback");
        AssertEx.Equal(
            100,
            SettingsJson.Deserialize("""{"scalePercent":999}""").ScalePercent,
            "invalid scale fallback");
        AssertEx.Equal(
            false,
            SettingsJson.Deserialize("""{"scalePercent":125,"alwaysOnTop":true}""").Muted,
            "missing mute uses safe default");
    }
}
