using Godot;

namespace DesktopPet.App;

public partial class PetRoot : Node2D
{
    public override void _Ready()
    {
        var window = GetWindow();
        window.TransparentBg = true;
        window.Borderless = true;
        window.AlwaysOnTop = false;
    }
}
