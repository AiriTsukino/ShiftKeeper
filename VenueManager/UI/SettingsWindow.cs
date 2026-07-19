using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using VenueManager.UI.Components;

namespace VenueManager.UI;

public sealed class SettingsWindow : Window
{
    private readonly MainWindow content;

    public SettingsWindow(MainWindow content)
        : base("VenueManager Settings###VenueManagerSettingsWindow", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoFocusOnAppearing)
    {
        this.content = content;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(820, 570),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void PreDraw() => VenueManagerTheme.Push();
    public override void PostDraw() => VenueManagerTheme.Pop();
    public override void Draw() => content.DrawSettings();
}
