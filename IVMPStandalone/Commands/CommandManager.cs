using Dalamud.Game.Command;
using IVPlugin.Core;
using IVPlugin.Log;
using IVPlugin.Services;
using IVPlugin.UI.Windows;
using System;
using System.Collections.Generic;

namespace IVPlugin.Commands
{
    public class CommandManager : IDisposable
    {
        public const string MainCommand = "/ivmp";

        public List<string> CommandList = new List<string>();
        public static CommandManager Instance { get; private set; } = null!;

        public Configuration configuration { get; private set; }

        public CommandManager()
        {
            configuration = IllusioVitae.configuration;

            DalamudServices.CommandManager.AddHandler(MainCommand, new CommandInfo(OnMainCommand)
            {
                HelpMessage = "Open IVMP Creator"
            });

            Instance = this;
        }

        private void OnMainCommand(string command, string args)
        {
            ModCreationWindow.Show();
        }

        public void RemoveCommands()
        {
            foreach (var command in CommandList)
            {
                DalamudServices.CommandManager.RemoveHandler(command);

                IllusioDebug.Log($"Remove Command {command}", LogType.Debug);
            }

            CommandList.Clear();
        }

        public void Dispose()
        {
            DalamudServices.CommandManager.RemoveHandler(MainCommand);

            RemoveCommands();

            Instance = null!;
        }
    }
}
