namespace DesktopPet.Core;

public sealed record PetSettings(
    int ScalePercent,
    bool AlwaysOnTop,
    bool Muted,
    int X,
    int Y,
    string MonitorId)
{
    public static PetSettings Default => new(100, false, false, -1, -1, "");
}
