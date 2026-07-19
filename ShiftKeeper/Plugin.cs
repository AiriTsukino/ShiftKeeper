using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Newtonsoft.Json;
using ShiftKeeper.Services;
using ShiftKeeper.UI;

namespace ShiftKeeper;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/shiftkeeper";
    private const string SettingsCommandName = "/shiftkeepersettings";
    private readonly WindowSystem windowSystem = new("ShiftKeeper");
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
        config = LoadConfiguration();
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

        var info = new CommandInfo(OnCommand) { HelpMessage = "Toggle the ShiftKeeper dashboard." };
        DalamudServices.CommandManager.AddHandler(CommandName, info);
        DalamudServices.CommandManager.AddHandler(SettingsCommandName, new CommandInfo(OnSettingsCommand) { HelpMessage = "Toggle ShiftKeeper settings." });
        DalamudServices.PluginInterface.UiBuilder.Draw += DrawUi;
        DalamudServices.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;
        DalamudServices.PluginInterface.UiBuilder.OpenConfigUi += ToggleSettingsUi;
        persistence.SaveNow();
    }

    private void OnCommand(string command, string arguments) => ToggleMainUi();
    private void OnSettingsCommand(string command, string arguments) => ToggleSettingsUi();

    private static Configuration LoadConfiguration()
    {
        if (DalamudServices.PluginInterface.GetPluginConfig() is Configuration current) return current;

        var configurationPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher",
            "pluginConfigs",
            "ShiftKeeper.json");
        try
        {
            if (File.Exists(configurationPath))
                return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(configurationPath)) ?? new Configuration();
        }
        catch (Exception ex)
        {
            DalamudServices.Log.Warning(ex, "ShiftKeeper could not load its configuration file.");
        }

        return new Configuration();
    }

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
