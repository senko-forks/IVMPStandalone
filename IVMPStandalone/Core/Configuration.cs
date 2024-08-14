using Dalamud.Configuration;
using System;

namespace IVPlugin.Core
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;
    }
}
