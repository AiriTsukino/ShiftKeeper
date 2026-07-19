using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using VenueManager.Services;
using VenueManager.UI;

namespace VenueManager;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/venuemanager";
    private const string SettingsCommandName = "/venuemanagersettings";
    private readonly WindowSystem windowSystem = new("VenueManager");
    private readonly Configuration config;
    private readonly PersistenceService persistence;
    private readonly StaffTrackingService tracking;
    private readonly TradePaymentService tradePayments;
    private readonly FileDialogService dialogs;
    private readonly TellWindow tellWindow;
    private readonly MainWindow mainWindow;
    private readonly SettingsWindow settingsWindow;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        DalamudServices.Initialize(pluginInterface);
        config = DalamudServices.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        if (config.Version < 3) config.Version = 3;
        persistence = new PersistenceService(config);
        tracking = new StaffTrackingService(config, persistence);
        tradePayments = new TradePaymentService(config, persistence);
        var chat = new ChatCommandService();
        var targeting = new TargetingService();
        dialogs = new FileDialogService();
        tellWindow = new TellWindow(chat);
        mainWindow = new MainWindow(config, persistence, tradePayments, targeting, dialogs, tellWindow.OpenFor, OpenSettingsWindow) { IsOpen = config.WindowVisible };
        settingsWindow = new SettingsWindow(mainWindow) { IsOpen = config.SettingsWindowVisible };
        windowSystem.AddWindow(mainWindow);
        windowSystem.AddWindow(settingsWindow);
        windowSystem.AddWindow(tellWindow);

        var info = new CommandInfo(OnCommand) { HelpMessage = "Toggle the VenueManager dashboard." };
        DalamudServices.CommandManager.AddHandler(CommandName, info);
        DalamudServices.CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand) { HelpMessage = "Toggle VenueManager settings." });
        DalamudServices.PluginInterface.UiBuilder.Draw += DrawUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUi;
        persistence.SaveNow();
    }

    private void OnCommand(string command, string arguments) => ToggleMainUi();
    private void OnSettingsCommand(string command, string arguments) => ToggleSettingsUi();

    private void ToggleMainUi()
    {
        mainWindow.IsOpen = !mainWindow.IsOpen;
        config.WindowVisible = mainWindow.IsOpen;
        persistence.SaveNow();
    }

    private void OpenSettingsWindow()
    {
        settingsWindow.IsOpen = true;
        config.SettingsWindowVisible = true;
        persistence.SaveNow();
    }

    private void ToggleSettingsUi()
    {
        settingsWindow.IsOpen = !settingsWindow.IsOpen;
        config.SettingsWindowVisible = settingsWindow.IsOpen;
        persistence.SaveNow();
    }

    private void DrawUi()
    {
        windowSystem.Draw();
        dialogs.Pump();
        if (config.WindowVisible != mainWindow.IsOpen || config.SettingsWindowVisible != settingsWindow.IsOpen)
        {
            config.WindowVisible = mainWindow.IsOpen;
            config.SettingsWindowVisible = settingsWindow.IsOpen;
            persistence.SaveNow();
        }
    }

    public void Dispose()
    {
        persistence.SaveNow();
        DalamudServices.PluginInterface.UiBuilder.Draw -= DrawUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi -= ToggleSettingsUi;
        DalamudServices.CommandManager.RemoveHandler(CommandName);
        DalamudServices.CommandManager.RemoveHandler(SettingsCommandName);
        windowSystem.RemoveAllWindows();
        dialogs.Dispose();
        tellWindow.Dispose();
        tradePayments.Dispose();
        tracking.Dispose();
        persistence.Dispose();
    }
}
