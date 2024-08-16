using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace IVPlugin.Mods.Structs
{
    public struct CustomEmote
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public string cameraPath { get; set; }

        public bool allowNPC { get; set; }

        public CustomBGMdata bgmData { get; set; }

        public List<CustomEmoteData> emoteData { get; set; }

        public List<DataPaths> SharedResources { get; set; }

        public ModCatagory? category { get; set; }

        public CustomEmote()
        {
            bgmData = new();
            emoteData = new List<CustomEmoteData>();
        }

        public Dictionary<string, string> GetBGMDictionary(string folderPath)
        {
            Dictionary<string, string> temp = new();

            if (bgmData.vfxPath != "" && bgmData.scdPath != "")
            {
                var trueSCDPath = Path.Combine(folderPath, bgmData.scdPath).Replace("\\", "/");
                var truevfxPath = Path.Combine(folderPath, bgmData.vfxPath).Replace("\\", "/");
                var trueOrcSCDPath = Path.Combine(folderPath, bgmData.orcScdPath).Replace("\\", "/");

                temp.Add($"sound/illusiovitae/bgm/{Path.GetFileNameWithoutExtension(bgmData.scdPath)}.scd".ToLower(), trueSCDPath);
                temp.Add($"sound/illusiovitae/bgm/{Path.GetFileNameWithoutExtension(bgmData.orcScdPath)}.scd".ToLower(), trueOrcSCDPath);
                temp.Add($"vfx/illusiovitae/bgm/{Path.GetFileNameWithoutExtension(bgmData.vfxPath)}.avfx".ToLower(), truevfxPath);
            }
            return temp;
        }

        public Dictionary<string, string> GetSharedResourcesDictionary(string folderPath)
        {
            Dictionary<string, string> temp = new();

            foreach(var path in SharedResources)
            {
                var realPath = Path.Combine(folderPath, path.LocalPath);

                temp.Add(path.GamePath, realPath);
            }

            return temp;
        }
    }

    [Serializable]
    public struct CustomEmoteData
    {
        public string emoteCommand { get ; set; }
        public int emoteID { get; set; }
        public bool isLooping { get; set; }
        public EmoteType? emoteType { get; set; }
        public string? tracklistPath { get; set; }

        public bool disableWeapon { get; set; }

        public List<DataPaths> dataPaths { get; set; }

        public List<CustomVFXData> vfxData { get; set; }

        public CustomEmoteData()
        {
            dataPaths = new List<DataPaths>();
            vfxData = new List<CustomVFXData>();
        }

        public string GetCommand()
        {
            if (!emoteCommand.StartsWith("/"))
            {
                return "/" + emoteCommand;
            }

            return emoteCommand;
        }

        public Dictionary<string, string> GetDictioanry(string folderPath)
        {
            var raceRegex = new Regex("c([0-9]{4})");

            Dictionary<string, string> temp = new();

            foreach (var path in dataPaths)
            {
                var fixedLocalPath = Path.Combine(folderPath, path.LocalPath).Replace("\\", "/");

                if(path.validRaces.HasFlag(RaceCodes.C0101))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0101"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0201))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0201"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0301))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0301"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0401))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0401"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0501))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0501"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0601))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0601"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0701))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0701"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0801))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0801"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C0901))
                    temp.Add(raceRegex.Replace(path.GamePath, "c0901"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1001))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1001"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1101))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1101"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1201))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1201"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1301))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1301"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1401))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1401"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1501))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1501"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1601))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1601"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1701))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1701"), fixedLocalPath);
                if (path.validRaces.HasFlag(RaceCodes.C1801))
                    temp.Add(raceRegex.Replace(path.GamePath, "c1801"), fixedLocalPath);


            }

            return temp;
        }

    }

    [Serializable]
    public struct CustomVFXData
    {
        public List<DataPaths> vfxDatapaths { get; set; }
        public RaceCodes validRaces { get; set; }
        public VFXType VFXType { get; set; }

        public Dictionary<string, string> GetVFXDictionary(string folderPath)
        {
            Dictionary<string, string> temp = new();

            foreach (var path in vfxDatapaths)
            {
                var fixedLocalPath = Path.Combine(folderPath, path.LocalPath.Replace("\\", "/"));
                temp.Add(path.GamePath, fixedLocalPath);
            }

            return temp;
        }
    }

    [Serializable]
    public struct CustomBGMdata
    {
        public string vfxPath { get; set; }
        public string scdPath { get; set; }
        public string orcScdPath { get; set; }
    }

    [Serializable]
    public struct DataPaths
    {
        public string GamePath { get; set; }
        public string LocalPath { get; set; }
        public RaceCodes validRaces { get; set; }
    }

    [Serializable]
    public class IVTracklist
    {
        public List<IVTrack> tracks { get; set; } = new List<IVTrack>();

        public List<IVTrack> GetTracksForFrame(int frame)
        {
            return tracks.Select(x => x).Where(x => x.Frame == frame).ToList();
        }
    }

    [Serializable]
    public struct IVTrack
    {
        public uint Frame { get; set; }
        public TrackType Type { get; set; }
        public float? Value { get; set; }
        public string? sValue { get; set; }

        public IVTrack(uint frame, TrackType type, float value, string svalue)
        {
            this.Frame = frame;
            this.Type = type;
            this.Value = value;
            this.sValue = svalue;
        }
    }

    public enum ModCatagory { Global, Male, Female, Straight, Bisexual, Gay, Lesbian}

    public enum TrackType { Expression, Transparency, FadeIn, FadeOut, Outfit, Scale, HideWeapons, ChangeTime, ChangeMonth, ChangeSkybox}

    public enum VFXType {actorVFX, staticVFX}

    [Flags]
    public enum RaceCodes : uint
    {
        [Description("Midlander Male")]
        C0101 = 1,
        [Description("Midlander Female")]
        C0201 = 2,
        [Description("Highlander Male")]
        C0301 = 4,
        [Description("Highlander Female")]
        C0401 = 8,
        [Description("Elezen Male")]
        C0501 = 16,
        [Description("Elezen Female")]
        C0601 = 32,
        [Description("Miqo'te Male")]
        C0701 = 64,
        [Description("Miqo'te Female")]
        C0801 = 128,
        [Description("Roegadyn Male")]
        C0901 = 256,
        [Description("Roegadyn Female")]
        C1001 = 512,
        [Description("Lalafell Male")]
        C1101 = 1024,
        [Description("Lalafell Female")]
        C1201 = 2048,
        [Description("Au'ra Male")]
        C1301 = 4096,
        [Description("Au'ra Female")]
        C1401 = 8192,
        [Description("Hrothgar Male")]
        C1501 = 16384,
        [Description("Hrothar Female")]
        C1601 = 32768,
        [Description("Viera Male")]
        C1701 = 65536,
        [Description("Viera Female")]
        C1801 = 131072,

        all = C0101 | C0201 | C0301 | C0401 | C0501 | C0601 | C0701 | C0801 | C0901 | C1001 | C1101 | C1201 | C1301 | C1401 | C1501 | C1601 | C1701 | C1801,
    }

    public static class RaceCodeExtensions
    {
        public static string? GetDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value);
            if (name != null)
            {
                var field = type.GetField(name);
                if (field != null)
                    return (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute)?.Description;
            }
            return null;
        }
    }

    [Flags]
    public enum EmoteType : byte {Full, Additive}
}
