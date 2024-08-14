using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Plugin;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using System;

namespace IVPlugin.UI
{
    public class WindowsManager : IDisposable
    {
        public static WindowsManager Instance { get; private set; } = null!;

        private readonly IDalamudPluginInterface pluginInterface;

        public FileDialogManager fileDialogManager;

        public WindowsManager(IDalamudPluginInterface _pluginInterface)
        {
            pluginInterface = _pluginInterface;

            fileDialogManager = new FileDialogManager();

            Instance = this;

            pluginInterface.UiBuilder.Draw += DrawUI;
            pluginInterface.UiBuilder.OpenMainUi += ModCreationWindow.Show;
            pluginInterface.UiBuilder.DisableGposeUiHide = true;
        }

        private void DrawUI()
        {
            if(!DalamudServices.clientState.IsLoggedIn) return;

            fileDialogManager.Draw();
            ModCreationWindow.Draw();
        }

        public void Dispose()
        {
            pluginInterface.UiBuilder.Draw -= DrawUI;
            pluginInterface.UiBuilder.OpenMainUi -= ModCreationWindow.Show;
            Instance = null!;
        }
    }
}
