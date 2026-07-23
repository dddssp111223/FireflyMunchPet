using System.Text.Json;

namespace DesktopPet.Core;

public static class SettingsJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Serialize(PetSettings value) =>
        JsonSerializer.Serialize(value, Options);

    public static PetSettings Deserialize(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<SettingsDto>(json, Options);
            if (dto is null)
                return PetSettings.Default;

            var defaults = PetSettings.Default;
            var requestedScale = dto.ScalePercent ?? defaults.ScalePercent;
            var scale = requestedScale is 75 or 100 or 125 or 150
                ? requestedScale
                : defaults.ScalePercent;

            return new PetSettings(
                scale,
                dto.AlwaysOnTop ?? defaults.AlwaysOnTop,
                dto.Muted ?? defaults.Muted,
                dto.X ?? defaults.X,
                dto.Y ?? defaults.Y,
                dto.MonitorId ?? defaults.MonitorId);
        }
        catch (JsonException)
        {
            return PetSettings.Default;
        }
    }

    private sealed record SettingsDto(
        int? ScalePercent,
        bool? AlwaysOnTop,
        bool? Muted,
        int? X,
        int? Y,
        string? MonitorId);
}
