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
        AssertEx.Equal(
            30,
            SettingsJson.Deserialize("""{"scalePercent":30}""").ScalePercent,
            "30 percent scale is valid");
        AssertEx.Equal(
            50,
            SettingsJson.Deserialize("""{"scalePercent":50}""").ScalePercent,
            "50 percent scale is valid");
        AssertEx.Equal(
            100,
            SettingsJson.Deserialize("""{"scalePercent":31}""").ScalePercent,
            "unknown scale falls back to default");
        AssertEx.Equal(
            false,
            SettingsJson.Deserialize("""{"scalePercent":100}""").HarmonizedMode,
            "legacy settings default to original visuals");
        AssertEx.Equal(
            true,
            SettingsJson.Deserialize("""{"harmonizedMode":true}""").HarmonizedMode,
            "harmonized mode is restored");
        AssertEx.Equal(
            false,
            SettingsJson.Deserialize("""{"scalePercent":100}""").RemindersEnabled,
            "legacy settings keep reminders disabled");
        AssertEx.Equal(
            true,
            SettingsJson.Deserialize("""{"remindersEnabled":true}""").RemindersEnabled,
            "reminder master switch is restored");
        AssertEx.True(
            SettingsJson.Serialize(PetSettings.Default with { RemindersEnabled = true })
                .Contains("\"remindersEnabled\":true", StringComparison.Ordinal),
            "reminder master switch is serialized");
    }
}
