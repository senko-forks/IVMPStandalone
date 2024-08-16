using Dalamud.Plugin;
using IVPlugin.Core;
using IVPlugin.UI;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.Commands;

namespace IVPlugin
{
    public sealed class IllusioVitae : IDalamudPlugin
    {
        public const string Name = "Illusio Vitae";

        public static string Version = $"0.1.0 Beta";

#if DEBUG
        public const bool IsDebug = true;
#else
        public const bool IsDebug = false;
#endif

        public static Configuration configuration { get; private set; } = null!;
        public GameResourceManager resourceHandler { get; private set; }
        public WindowsManager windowsManager { get; private set; }
        public CommandManager commandManager { get; private set; }

        public IllusioVitae(IDalamudPluginInterface pluginInterface)
        {
            DalamudServices.Initialize(pluginInterface);
            configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            resourceHandler = new(pluginInterface);
            commandManager = new();
            windowsManager = new(pluginInterface);

            if (IsDebug && DalamudServices.PluginInterface.Reason == PluginLoadReason.Reload)
                UI.Windows.ModCreationWindow.IsOpen = true;
        }

        public static bool InDebug()
        {
            return IsDebug;
        }

        public void Dispose()
        {
            DalamudServices.PluginInterface.SavePluginConfig(configuration);

            commandManager.Dispose();
            resourceHandler.Dispose();
            windowsManager.Dispose();
        }
    }
}
