using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using IVPlugin.Services;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IVPlugin.Resources
{
    public unsafe class GameResourceManager : IDisposable
    {
        public static GameResourceManager Instance { get; private set; } = null!;

        private readonly Dictionary<string, object> cachedDocuments = new();
        private readonly Dictionary<string, IDalamudTextureWrap> cachedImages = new();

        public readonly IReadOnlyDictionary<uint, ActionTimeline> ActionTimelines;
        public readonly IReadOnlyDictionary<uint, Emote> Emotes;
        public readonly IReadOnlyDictionary<uint, ActionTimeline> BlendEmotes;
        public readonly IReadOnlyDictionary<uint, Lumina.Excel.GeneratedSheets.Action> Actions;

        public GameResourceManager(IDalamudPluginInterface pluginInterface)
        {
            Instance = this;

            ActionTimelines = DalamudServices.DataManager.GetExcelSheet<ActionTimeline>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            Emotes = DalamudServices.DataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();

            BlendEmotes = DalamudServices.DataManager.GetExcelSheet<Emote>()!.ToDictionary(x => x.RowId, x => x.ActionTimeline[4].Value).AsReadOnly();

            Actions = DalamudServices.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()!.ToDictionary(x => x.RowId, x => x).AsReadOnly();
        }

        public IDalamudTextureWrap GetResourceImage(string name)
        {
            if (cachedImages.TryGetValue(name, out var cached))
                return cached;

            using var stream = GetRawResourceStream($"Images.{name}");
            using var reader = new BinaryReader(stream);
            var imgBin = reader.ReadBytes((int)stream.Length);
            var img = DalamudServices.textureProvider.CreateFromImageAsync(imgBin).Result;
            cachedImages[name] = img;
            return img;
        }

        public byte[] GetResourceByteFile(string name)
        {
            using var stream = GetRawResourceStream($"Files.{name}");
            using var reader = new BinaryReader(stream);
            var fileBytes = reader.ReadBytes((int)stream.Length);

            return fileBytes;
        }

        public string GetResourceStringFile(string name)
        {
            using var stream = GetRawResourceStream($"Files.{name}");
            using var reader = new StreamReader(stream);
            var file = reader.ReadToEnd();

            return file;
        }

        private Stream GetRawResourceStream(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"IVPlugin.Resources.Embedded.{name}";
            var stream = assembly.GetManifestResourceStream(resourceName);
            return stream ?? throw new Exception($"Resource {name} not found.");
        }





        public void Dispose()
        {
            foreach (var img in cachedImages.Values)
                img?.Dispose();

            cachedImages?.Clear();
            cachedDocuments?.Clear();
        }
    }

    public struct ENPCExtended
    {
        public ENpcBase Base;
        public ENpcResident resident;
    }
}
