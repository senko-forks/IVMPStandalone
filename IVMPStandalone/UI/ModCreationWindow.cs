using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using ImGuiNET;
using IVPlugin.Json;
using IVPlugin.Mods.Structs;
using IVPlugin.Resources;
using IVPlugin.Services;
using IVPlugin.UI.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace IVPlugin.UI.Windows
{
    public static class ModCreationWindow
    {
        public static bool IsOpen = false;
        public static void Show() => IsOpen = true;
        public static void Toggle() => IsOpen = !IsOpen;

        public static string ModName = string.Empty, ModAuthor = string.Empty, CamPath = string.Empty;
        public static ModCatagory selectedCatagory = ModCatagory.Global;
        public static bool allowNPC = true, allowNSFW;

        public static int emoteCount = 0;

        public static ModSharedResourceTab sharedResources = new();

        public static List<ModEmoteTab> emotes = new() { new() };

        private static string SaveAsDefaultName = "NewMod.ivmp";
        public static string? ImportedPMPPath = null;

        private static void ShowNotification(string msg, bool isError = true)
        {
            DalamudServices.notificationManager.AddNotification(new Notification()
            {
                Title = "IVMP Mod Creation",
                Type = isError ? NotificationType.Error : NotificationType.Info,
                Content = msg
            });
            Log.IllusioDebug.Log(msg, isError ? Log.LogType.Warning : Log.LogType.Info, !isError);
        }

        public static void DrawTruncatedLocalPath(ref string localPath)
        {
            if (ImportedPMPPath == null)
            {
                ImGui.Text(localPath);
                return;
            }

            bool clicked = false;

            if (localPath.StartsWith(ImportedPMPPath))
            {
                string tmp = "PMP:" + localPath.Substring(ImportedPMPPath.Length);
                string tmp2 = tmp;
                ImGui.InputText("##LocalpathInput", ref tmp, 500);
                ImGui.SameLine();
                clicked = ImGui.Button("Browse##LocalPath");

                if (tmp != tmp2)
                {
                    if (tmp.Substring(0, 4) == "PMP:")
                        localPath = ImportedPMPPath + tmp.Substring(4);
                    else
                        localPath = tmp;
                }
            }
            else
            {
                ImGui.InputText("##LocalpathInput", ref localPath, 500);
                ImGui.SameLine();
                clicked = ImGui.Button("Browse##LocalPath");
            }

            if (clicked)
            {
                WindowsManager.Instance.fileDialogForPMPManager.OpenFileDialog("Square File", ".*", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    // XXX BAD BROKEN AAAAAAA FIXME
                    //localPath = FilePath;
                });
            }
        }

        public static void Draw()
        {
            if (!IsOpen) return;

            var size = new Vector2(1000, 500);
            ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);

            ImGui.SetNextWindowSizeConstraints(new(1000, 500), new Vector2(9999, 9999));

            if (ImGui.Begin("IVMP Mod Creation (IVMPStandalone)", ref IsOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ShowMeta();
                ShowEmotes();
            }
        }

        private static void ShowMeta()
        {
            ImGui.BeginGroup();
            ImGui.Text("Mod Name:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##ModNameInput", ref ModName, 100);
            ImGui.Spacing();
            ImGui.Text("Mod Author:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##ModAuthorInput", ref ModAuthor, 200);
            ImGui.Spacing();
            ImGui.Text("Camera File:");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("##CameraFileInput", ref CamPath, 5000);
            ImGui.SameLine();
            if (ImGui.Button("Browse##.xcpbrowse"))
            {
                WindowsManager.Instance.fileDialogForPMPManager.OpenFileDialog("Import Camera File", ".xcp", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    CamPath = FilePath;
                });
            }
            ImGui.Text("Allow NPCS");
            ImGui.SameLine(90);
            ImGui.SetNextItemWidth(100);
            ImGui.Checkbox("##NPCCheck", ref allowNPC);
            ImGui.SameLine();
            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allow the use of custom actors for this mod");
            }
            ImGui.EndGroup();

            ImGui.SameLine(400);

            var currentCatagory = Enum.GetName<ModCatagory>(selectedCatagory);

            ImGui.SetNextItemWidth(100);

            using (var combo = ImRaii.Combo("##ModType", currentCatagory))
            {
                if (combo.Success)
                {
                    var catagories = Enum.GetNames(typeof(ModCatagory)).ToList();

                    if(!allowNSFW)
                        catagories.RemoveRange(3, 4);

                    foreach (var catagory in catagories)
                    {
                        if (ImGui.Selectable(catagory, catagory == currentCatagory))
                        {
                            selectedCatagory = Enum.Parse<ModCatagory>(catagory);
                        }
                    }
                }
            }

            ImGui.SameLine();

            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Catagory Sets the type of emote it is. Most things are global for example but would \n set it to Male if you only support male animations. This has an effect on the modlist within the plugin.");
            }

            ImGui.SameLine();

            ImGui.Checkbox("Use NSFW Categories", ref allowNSFW);

            if (ImGui.Button("Import pmp file"))
            {
                ParsePMP();
            }

            ImGui.SameLine();

            if(ImGui.Button("Export IVMP"))
            {
                CreateIVMP();
            }

            ImGui.SameLine();

            if(ImGui.Button("Reset Fields"))
            {
                ResetFields();
            }
        }

        private static void ResetFields()
        {
            ModName = "";
            ModAuthor = "";
            CamPath = "";
            selectedCatagory = ModCatagory.Global;
            allowNPC = true;
            sharedResources = new() { paths = new()};
            emotes = new() { new() };
            WindowsManager.Instance.fileDialogForPMPManager.Reset();
            SaveAsDefaultName = "NewMod.ivmp";
        }

        private static void ShowEmotes()
        {
            if(ImGui.BeginTabBar("Resource List"))
            {
                if (ImGui.BeginTabItem("Shared Resources"))
                {
                    sharedResources.Draw();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Emotes"))
                {
                    if (ImGui.Button("Add Emote"))
                    {
                        emotes.Add(new());
                    }

                    if (ImGui.BeginTabBar("Emote List"))
                    {
                        for(var i = 0; i < emotes.Count; i++)
                        {
                            var currentEmote = emotes[i];

                            if (currentEmote == null) currentEmote = new();

                            if(ImGui.BeginTabItem($"Emote {i}"))
                            {
                                currentEmote.Draw();

                                ImGui.EndTabItem();
                            }
                        }
                        ImGui.EndTabBar();
                    }

                    ImGui.EndTabItem();
                }

                if(ImGui.BeginTabItem("CM Calls"))
                {
                    TracklistGenerator.Draw();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        internal struct SHA1HashKey
        {
            public ulong A;
            public ulong B;
            public uint C;

            public SHA1HashKey(byte[] data)
            {
                A = BitConverter.ToUInt64(data, 0);
                B = BitConverter.ToUInt64(data, 8);
                C = BitConverter.ToUInt32(data, 16);
            }
        }

        private static void ParsePMP()
        {
            WindowsManager.Instance.fileDialogManager.OpenFileDialog("PMP To Convert", ".pmp", (Confirm, FilePath) =>
            {
                if (!Confirm) return;
                ResetFields();
                DoParsePMP(FilePath);
            });
        }

        private static void DoParsePMP(string FilePath)
        {
            try
            {
                Dictionary<string, string> filePaths = new Dictionary<string, string>();
                var FinalPath = Path.Combine(DalamudServices.PluginInterface.ConfigDirectory.FullName, "PMP");

                if (Directory.Exists(FinalPath))
                {
                    Directory.Delete(FinalPath, true);
                }

                WindowsManager.Instance.fileDialogForPMPManager.CustomSideBarItems.Add(new ()
                {
                    Name = "Imported PMP",
                    Path = FinalPath,
                    Icon = FontAwesomeIcon.Folder,
                    Position = 999
                });

                ImportedPMPPath = FinalPath;

                SaveAsDefaultName = Path.GetFileNameWithoutExtension(FilePath) + ".ivmp";

                var TempPmp = Directory.CreateDirectory(FinalPath);

                using(var zip = new ZipArchive(File.Open(FilePath, FileMode.Open, FileAccess.Read)))
                {
                    zip.ExtractToDirectory(FinalPath);
                }

                foreach(var file in Directory.GetFiles(FinalPath))
                {
                    if (Path.GetExtension(file) != ".json") continue;

                    var filename = Path.GetFileName(file);

                    if (filename == "meta.json")
                    {
                        var meta = JsonHandler.Deserialize<PMPmeta>(File.ReadAllText(file));

                        ModName = meta.Name;
                        ModAuthor = meta.Author;

                        continue;
                    }

                    if(filename == "default_mod.json")
                    {
                        var mod = JsonHandler.Deserialize<PMPMod>(File.ReadAllText(file));

                        if(mod.Files != null && mod.Files.Count > 0)
                        {
                            foreach(var modfile in mod.Files)
                            {
                                DalamudServices.log.Info($"Add from default: GamePath={modfile.Key}");
                                filePaths.Add(modfile.Key, modfile.Value);
                            }
                        }

                        continue;
                    }

                    if (!filename.Contains("group_", StringComparison.OrdinalIgnoreCase)) continue;

                    var group = JsonHandler.Deserialize<PMPGroup>(File.ReadAllText(file));

                    if(group.Options != null && group.Options.Count > 0)
                    {
                        foreach(var groupfile in group.Options)
                        {
                            foreach(var files in groupfile.Files)
                            {
                                DalamudServices.log.Info($"Add from group: GamePath={files.Key}");
                                filePaths.TryAdd(files.Key, files.Value);
                            }
                        }
                    }
                }

                // Unique animation keys (i.e. filenames) will create separate emotes
                // Unique file contents will create different racial variants
                Dictionary<string, ModEmoteTab> uniquePaps = new();
                Dictionary<(string, SHA1HashKey), DataPathsUI> uniqueContents = new();

                emotes.Clear();

                foreach (var file in filePaths.Keys)
                {
                    var localPath = Path.Combine(FinalPath, filePaths[file]);

                    if (Path.GetExtension(file) != ".pap")
                    {
                        sharedResources.paths.Add(new() { validRaces = RaceCodes.all, GamePath = file, LocalPath = localPath});
                        continue;
                    }

                    var (animID, isLooping) = DetectAnim(file);

                    // Assume an unknown animation is a shared resource to be referenced, such as a facial animation
                    if (animID == 0)
                    {
                        // It doesn't seem worth restricting the race IDs here but I could be wrong?
                        var validRaces = RaceCodes.all;
                        sharedResources.paths.Add(new() { validRaces = validRaces, GamePath = file, LocalPath = localPath});
                        continue;
                    }

                    // This fails to detect job-specific animations as being unique
                    var papName = Path.GetFileName(file);
                    var papHash = new SHA1HashKey(SHA1.HashData(File.ReadAllBytes(localPath)));

                    bool isNewPapHash = !uniqueContents.TryGetValue((papName, papHash), out var dataPath);
                    bool isNewPapName = !uniquePaps.TryGetValue(papName, out var tab);

                    if (isNewPapHash)
                    {
                        dataPath = new()
                        {
                            GamePath = file,
                            LocalPath = localPath,
                            validRaces = (RaceCodes)0
                        };
                        uniqueContents.Add((papName, papHash), dataPath);
                    }

                    // This either populates the initial races, or adds more races to an existing path
                    GetRaceCodeFromPath(ref dataPath.validRaces, file);

                    if (isNewPapName && animID != 0)
                    {
                        if (animID != 0)
                        {
                            DalamudServices.log.Info($"Adding new emote: GamePath={file}");

                            var commandName = $"{ModName}{uniquePaps.Count+1}";

                            commandName = Regex.Replace(commandName, "[^A-Za-z0-9]", "").ToLowerInvariant();

                            tab = new() {Command = commandName, animID = animID, paths = new() { dataPath }, isLooping = isLooping };

                            emotes.Add(tab);
                            uniquePaps.Add(papName, tab);
                        }
                    }
                    else if (isNewPapHash)
                    {
                        DalamudServices.log.Info($"Adding race variant: GamePath={file}");
                        tab.paths.Add(dataPath);
                    }
                }
            }
            catch (Exception ex)
            {
                DalamudServices.log.Error(ex, "Error during PMP import");
                ShowNotification("PMP import failed. See /xllog for more info");
            }
        }

        public static void CreateIVMP()
        {
            CustomEmote file = new();

            file.Name = ModName;
            file.Author = ModAuthor;
            file.cameraPath = CamPath;
            file.allowNPC = allowNPC;
            file.category = selectedCatagory;

            file.emoteData = new();

            file.SharedResources = new();

            foreach (var path in sharedResources.paths)
            {
                if (string.IsNullOrEmpty(path.GamePath) || string.IsNullOrEmpty(path.LocalPath))
                {
                    ShowNotification("Unfilled Fields detected in shared resources please either fix or remove");
                    return;
                }

                file.SharedResources.Add(new() { GamePath = path.GamePath, LocalPath = path.LocalPath });
            }

            if (emotes.Count == 0)
            {
                ShowNotification("No emotes have been created!");
                return;
            }

            for (var i = 0; i < emotes.Count; i++)
            {
                ModEmoteTab currentEmoteData = emotes[i];

                CustomEmoteData tempData = new();

                tempData.emoteCommand = currentEmoteData.Command;

                if (currentEmoteData.Command.IsNullOrEmpty())
                {
                    ShowNotification($"Emote #{i + 1} is missing an Emote Command!");
                    return;
                }

                if (currentEmoteData.animID <= 0)
                {
                    ShowNotification($"Emote #{i + 1} has not been assigned an Animation ID!");
                    return;
                }

                tempData.emoteID = currentEmoteData.animID;
                tempData.isLooping = currentEmoteData.isLooping;
                tempData.disableWeapon = currentEmoteData.hideWeapon;
                tempData.emoteType = currentEmoteData.currentType;
                tempData.tracklistPath = currentEmoteData.tracklistPath;

                tempData.dataPaths = new();
                tempData.vfxData = new();

                for (var x = 0; x < currentEmoteData.paths.Count; x++)
                {
                    DataPathsUI currentPaths = currentEmoteData.paths[x];

                    if (string.IsNullOrEmpty(currentPaths.GamePath) && string.IsNullOrEmpty(currentPaths.LocalPath))
                    {
                        ShowNotification($"Emote #{i + 1}, Data Path #{x + i} is missing information!");
                        return;
                    }

                    if (string.IsNullOrEmpty(currentPaths.GamePath) && !string.IsNullOrEmpty(currentPaths.LocalPath))
                    {
                        ShowNotification($"Emote #{i + 1}, Data Path #{x + i} is missing information!");
                        return;
                    }

                    if (!string.IsNullOrEmpty(currentPaths.GamePath) && string.IsNullOrEmpty(currentPaths.LocalPath))
                    {
                        ShowNotification($"Emote #{i + 1}, Data Path #{x + i} is missing information!");
                        return;
                    }

                    var raceMask = (RaceCodes)0;
                    var raceMaskDuplicate = (RaceCodes)0;

                    foreach (var path in tempData.dataPaths)
                    {
                        var dupeMask = (raceMask & path.validRaces);
                        raceMaskDuplicate |= dupeMask;

                        if (!path.GamePath.IsNullOrEmpty())
                            raceMask |= path.validRaces;
                    }

                    // For some reason having this causes the IV plugin to totally bug out
                    if (raceMaskDuplicate != (RaceCodes)0)
                    {
                        ShowNotification($"Emote #{i + 1} contains duplicate race assignments!");
                        return;
                    }

                    DataPaths tempPaths = new()
                    {
                        GamePath = currentPaths.GamePath,
                        LocalPath = currentPaths.LocalPath,
                        validRaces = currentPaths.validRaces,
                    };

                    tempData.dataPaths.Add(tempPaths);
                }

                for (var x = 0; x < currentEmoteData.vFXDatas.Count; x++)
                {
                    var currentVFXData = currentEmoteData.vFXDatas[x];

                    CustomVFXData tempVFXData = new()
                    {
                        VFXType = currentVFXData.vfxType,
                        validRaces = currentVFXData.validRaces,
                    };

                    tempVFXData.vfxDatapaths = new();

                    for (var y = 0; y < currentVFXData.vfxPaths.Count; y++)
                    {
                        var currentVFXPaths = currentVFXData.vfxPaths[y];

                        if (string.IsNullOrEmpty(currentVFXPaths.GamePath) && string.IsNullOrEmpty(currentVFXPaths.LocalPath))
                        {
                            ShowNotification($"Emote #{i + 1}, VFX #{x + i} Path #{y + i}is missing information!");
                            return;
                        }

                        if (string.IsNullOrEmpty(currentVFXPaths.GamePath) && !string.IsNullOrEmpty(currentVFXPaths.LocalPath))
                        {
                            ShowNotification($"Emote #{i + 1}, VFX #{x + i} Path #{y + i}is missing information!");
                            return;
                        }

                        if (!string.IsNullOrEmpty(currentVFXPaths.GamePath) && string.IsNullOrEmpty(currentVFXPaths.LocalPath))
                        {
                            ShowNotification($"Emote #{i + 1}, VFX #{x + i} Path #{y + i}is missing information!");
                            return;
                        }

                        DataPaths vfxDataPath = new()
                        {
                            GamePath = currentVFXPaths.GamePath,
                            LocalPath = currentVFXPaths.LocalPath,
                        };

                        tempVFXData.vfxDatapaths.Add(vfxDataPath);
                    }


                    tempData.vfxData.Add(tempVFXData);
                }

                file.emoteData.Add(tempData);
            }

            WindowsManager.Instance.fileDialogManager.SaveFileDialog("Select a location to Save the Illusio Vitae Modpack", ".ivmp", SaveAsDefaultName, ".ivmp", (Confirm, FilePath) =>
            {
                if (!Confirm) return;
                WriteIVMP(file, Path.GetDirectoryName(FilePath) + "\\" + Path.GetFileNameWithoutExtension(FilePath) + ".ivmp");
            });
        }

        public static void WriteIVMP(CustomEmote data, string savePath)
        {
            if (File.Exists(savePath)) { File.Delete(savePath); }

            try
            {
                using (ZipArchive archive = ZipFile.Open(savePath, ZipArchiveMode.Create))
                {
                    var addedFiles = new HashSet<string>();

                    // Silently accepts duplicate entries without added a second copy of the file
                    void TryAddArchiveFile(string diskPath, string zipPath)
                    {
                        if (!string.IsNullOrEmpty(diskPath) && !addedFiles.Contains(zipPath))
                        {
                            addedFiles.Add(zipPath);
                            archive.CreateEntryFromFile(diskPath, zipPath);
                        }
                    }

                    for (var i = 0; i < data.SharedResources.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(data.SharedResources[i].LocalPath))
                        {
                            TryAddArchiveFile(data.SharedResources[i].LocalPath, $"Shared/" + Path.GetFileName(data.SharedResources[i].LocalPath));
                        }

                        DataPaths resourceData = data.SharedResources[i];

                        resourceData.LocalPath = $"Shared/" + Path.GetFileName(data.SharedResources[i].LocalPath);

                        data.SharedResources[i] = resourceData;
                    }

                    for (var i = 0; i < data.emoteData.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(data.emoteData[i].tracklistPath))
                        {
                            var tracklist = JsonHandler.Deserialize<IVTracklist>(File.ReadAllText(data.emoteData[i].tracklistPath));

                            for (var x = 0; x < tracklist.tracks.Count; x++)
                            {
                                if (tracklist.tracks[x].Type == TrackType.Outfit)
                                {
                                    if (!string.IsNullOrEmpty(tracklist.tracks[x].sValue))
                                    {
                                        TryAddArchiveFile(tracklist.tracks[x].sValue, $"Mod{i}/" + Path.GetFileName(tracklist.tracks[x].sValue));

                                        var tempTrack = tracklist.tracks[x];
                                        tempTrack.sValue = $"Mod{i}/" + Path.GetFileName(tracklist.tracks[x].sValue);

                                        tracklist.tracks[x] = tempTrack;
                                    }
                                }
                            }

                            var entry = archive.CreateEntry($"Mod{i}/" + Path.GetFileName(data.emoteData[i].tracklistPath));

                            var fixedTracklist = JsonHandler.Serialize(tracklist);

                            using (var stream = entry.Open())
                            {
                                stream.Write(Encoding.ASCII.GetBytes(fixedTracklist));
                            };

                            CustomEmoteData tempData = data.emoteData[i];

                            tempData.tracklistPath = $"Mod{i}/" + Path.GetFileName(data.emoteData[i].tracklistPath);

                            data.emoteData[i] = tempData;
                        }

                        // Organize pap files in to sub-folders only if neccessary (racial variants)
                        var papFileSet = new HashSet<string>();
                        bool needSubMod = false;

                        for (var ii = 0; ii < data.emoteData[i].dataPaths.Count; ii++)
                        {
                            var papFile = Path.GetFileName(data.emoteData[i].dataPaths[ii].GamePath);
                            if (papFileSet.Contains(papFile))
                            {
                                needSubMod = true;
                                break;
                            }
                            papFileSet.Add(papFile);
                        }

                        for (var ii = 0; ii < data.emoteData[i].dataPaths.Count; ii++)
                        {
                            var subModFragment = needSubMod ? $"{ii+1}_" : string.Empty;
                            if (!string.IsNullOrEmpty(data.emoteData[i].dataPaths[ii].LocalPath))
                            {
                                TryAddArchiveFile(data.emoteData[i].dataPaths[ii].LocalPath, $"Mod{i}/" + subModFragment + Path.GetFileName(data.emoteData[i].dataPaths[ii].LocalPath));
                            }

                            DataPaths emoteData = data.emoteData[i].dataPaths[ii];

                            emoteData.LocalPath = $"Mod{i}/" + subModFragment + Path.GetFileName(data.emoteData[i].dataPaths[ii].LocalPath);

                            data.emoteData[i].dataPaths[ii] = emoteData;
                        }

                        for (var ii = 0; ii < data.emoteData[i].vfxData.Count; ii++)
                        {
                            for (var x = 0; x < data.emoteData[i].vfxData[ii].vfxDatapaths.Count; x++)
                            {
                                if (!string.IsNullOrEmpty(data.emoteData[i].vfxData[ii].vfxDatapaths[x].LocalPath))
                                {
                                    TryAddArchiveFile(data.emoteData[i].vfxData[ii].vfxDatapaths[x].LocalPath, $"VFX{i}/SubVFX{ii}/" + Path.GetFileName(data.emoteData[i].vfxData[ii].vfxDatapaths[x].LocalPath));
                                }

                                DataPaths vfxData = data.emoteData[i].vfxData[ii].vfxDatapaths[x];

                                vfxData.LocalPath = $"VFX{i}/SubVFX{ii}/" + Path.GetFileName(data.emoteData[i].vfxData[ii].vfxDatapaths[x].LocalPath);

                                data.emoteData[i].vfxData[ii].vfxDatapaths[x] = vfxData;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(data.cameraPath))
                    {
                        TryAddArchiveFile(data.cameraPath, "Camera/" + Path.GetFileName(data.cameraPath));

                        data.cameraPath = $"Camera/{Path.GetFileName(data.cameraPath)}";
                    }

                    CustomBGMdata bgmData = new();

                    if (!string.IsNullOrEmpty(data.bgmData.vfxPath))
                    {
                        TryAddArchiveFile(data.bgmData.vfxPath, "BGM/" + Path.GetFileName(data.bgmData.vfxPath));

                        bgmData.vfxPath = $"BGM/{Path.GetFileName(data.bgmData.vfxPath)}";
                    }

                    if (!string.IsNullOrEmpty(data.bgmData.scdPath))
                    {
                        TryAddArchiveFile(data.bgmData.scdPath, "BGM/" + Path.GetFileName(data.bgmData.scdPath));

                        bgmData.scdPath = $"BGM/{Path.GetFileName(data.bgmData.scdPath)}";
                    }

                    if (!string.IsNullOrEmpty(data.bgmData.orcScdPath))
                    {
                        TryAddArchiveFile(data.bgmData.orcScdPath, "BGM/" + Path.GetFileName(data.bgmData.orcScdPath));

                        bgmData.orcScdPath = $"BGM/{Path.GetFileName(data.bgmData.orcScdPath)}";
                    }


                    data.bgmData = bgmData;

                    var jsonFile = archive.CreateEntry("meta.data");

                    using (Stream st = jsonFile.Open())
                    {
                        using (StreamWriter sw = new StreamWriter(st))
                        {
                            sw.Write(JsonHandler.Serialize(data));
                        }
                    }

                    ShowNotification($"{System.IO.Path.GetFileName(savePath)} exported", false);
                }
            }
            catch (Exception ex)
            {
                DalamudServices.log.Error(ex, "Error during IVMP creation");
                ShowNotification("Files are Missing or destination IVMP is in use. See /xllog for more info");
            }
        }

        private static void GetRaceCodeFromPath(ref RaceCodes validRaces, string filePath)
        {
            if (filePath.Contains("c0101")) validRaces |= RaceCodes.C0101;
            if (filePath.Contains("c0201")) validRaces |= RaceCodes.C0201;
            if (filePath.Contains("c0301")) validRaces |= RaceCodes.C0301;
            if (filePath.Contains("c0401")) validRaces |= RaceCodes.C0401;
            if (filePath.Contains("c0501")) validRaces |= RaceCodes.C0501;
            if (filePath.Contains("c0601")) validRaces |= RaceCodes.C0601;
            if (filePath.Contains("c0701")) validRaces |= RaceCodes.C0701;
            if (filePath.Contains("c0801")) validRaces |= RaceCodes.C0801;
            if (filePath.Contains("c0901")) validRaces |= RaceCodes.C0901;
            if (filePath.Contains("c1001")) validRaces |= RaceCodes.C1001;
            if (filePath.Contains("c1101")) validRaces |= RaceCodes.C1101;
            if (filePath.Contains("c1201")) validRaces |= RaceCodes.C1201;
            if (filePath.Contains("c1301")) validRaces |= RaceCodes.C1301;
            if (filePath.Contains("c1401")) validRaces |= RaceCodes.C1401;
            if (filePath.Contains("c1501")) validRaces |= RaceCodes.C1501;
            if (filePath.Contains("c1601")) validRaces |= RaceCodes.C1601;
            if (filePath.Contains("c1701")) validRaces |= RaceCodes.C1701;
            if (filePath.Contains("c1801")) validRaces |= RaceCodes.C1801;
        }

        private static (int animID, bool isLooping) DetectAnim(string papPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(papPath);
            // Match the entire end of the path before falling back to more vague logic to avoid some ambiguous cases:
            //   "emote/s_goodbye_st" vs "emote/goodbye_st"
            //   "emote/angry_st" vs "facial/pose/angry_st"
            var anim = GameResourceManager.Instance.ActionTimelines.FirstOrDefault(x => x.Value.Key.RawString.Length > 0 && papPath.EndsWith($"{x.Value.Key.RawString}.pap", StringComparison.OrdinalIgnoreCase));
            if (anim.Key == default)
                anim = GameResourceManager.Instance.ActionTimelines.FirstOrDefault(x => x.Value.Key.RawString.Contains($"/{fileName}", StringComparison.OrdinalIgnoreCase));
            if (anim.Key == default)
                anim = GameResourceManager.Instance.ActionTimelines.FirstOrDefault(x => x.Value.Key.RawString.Contains(fileName, StringComparison.OrdinalIgnoreCase));

            if(anim.Value != null)
            {
                return (
                    (int)anim.Value.RowId,
                    anim.Value.IsLoop
                );
            }

            return (0, false);
        }

        public struct PMPMod
        {
            public int Version { get; set; }
            public Dictionary<string, string> Files { get; set; }
            public Dictionary<string, string> FileSwaps { get; set; }
        }

        public struct PMPmeta
        {
            public int FileVersion { get; set; }
            public string Name { get; set; }
            public string Author { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
            public string Version { get; set; }
            public string Website { get; set; }
            public List<string> ModTags { get; set; }
        }

        public struct PMPGroup
        {
            public int Version { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Image { get; set; }
            public int Priority { get; set; }
            public int DefaultSettings { get; set; }
            public List<PMPOption> Options { get; set; }
        }

        public struct PMPOption
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public int Priority { get; set; }
            public Dictionary<string, string> Files { get; set; }
            public Dictionary<string, string> FileSwaps { get; set; }
        }
    }

    public class ModSharedResourceTab
    {
        public List<DataPathsUI> paths = new List<DataPathsUI>();

        public void Draw()
        {
            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This tab is for files that are shared between emote.");
            }

            ImGui.Spacing();

            if(ImGui.Button("Add Data path"))
            {
                paths.Add(new());
            }

            using (var dataPaths = ImRaii.Child("##SharedDataPaths", new(0)))
            {
                if (dataPaths.Success)
                {
                    var tempList = paths.ToList();

                    for (var i = 0; i < tempList.Count; i++)
                    {
                        tempList[i].Draw(i, null, this);
                    }
                }
            }

        }
    }

    public class ModEmoteTab
    {
        public string Command = string.Empty, tracklistPath = string.Empty, EmoteSearch = string.Empty;
        public int animID = 0;
        public EmoteType currentType = EmoteType.Full;
        public bool isLooping = false, hideWeapon = false;

        public List<DataPathsUI> paths = new List<DataPathsUI>();
        public List<VFXData> vFXDatas = new List<VFXData>();

        private string[] animationTypes = ["Base", "Startup", "Floor", "Sitting", "Blend", "Other", "Adjusted"];

        // Cached values to avoid re-calculating every frame
        private int correlatedAnimID = -1;
        private string correlatedAnimName = "";
        private List<string> correlatedEmoteList = new();
        private bool animHasAnyOverride = false;

        private void CalculateAnimData()
        {
            if (animID != correlatedAnimID)
            {
                correlatedEmoteList.Clear();
                correlatedAnimID = animID;

                GameResourceManager.Instance.ActionTimelines.TryGetValue((uint)correlatedAnimID, out var anim);
                correlatedAnimName = anim?.Key ?? "";

                foreach (var emote in GameResourceManager.Instance.Emotes)
                {
                    for (int i = 0; i < animationTypes.Length; ++i)
                    {
                        if (animID != 0 && emote.Value.ActionTimeline[i].Row == animID)
                        {
                            if (emote.Value.Name == string.Empty)
                                correlatedEmoteList.Add($"Emote#{emote.Value.RowId} ({animationTypes[i]})");
                            else
                                correlatedEmoteList.Add($"{emote.Value.Name} ({animationTypes[i]})");
                        }
                    }
                }
            }

            animHasAnyOverride = false;

            foreach (var path in paths)
            {
                if (path.GamePath.StartsWith("chara/") && path.GamePath.EndsWith(correlatedAnimName + ".pap"))
                    animHasAnyOverride = true;
            }
        }

        private void DisplayAnimData()
        {
            if (!correlatedAnimName.IsNullOrEmpty())
            {
                ImGui.Text("Animation:");
                ImGui.SameLine();
                ImGui.Text(correlatedAnimName);

                if (!animHasAnyOverride)
                {
                    using var pushColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF8080FF);
                    ImGui.Text("No Pap file seems to apply to this animation");
                }
            }
            else
            {
                using var pushColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF8080FF);
                ImGui.Text("Animation ID does not seem to be valid");
            }

            ImGui.Text("Emote: ");
            ImGui.SameLine();

            if (correlatedEmoteList.Count == 0)
                ImGui.Text("Unknown");

            for (int i = 0; i < correlatedEmoteList.Count; ++i)
                ImGui.Text(correlatedEmoteList[i]);
        }

        public void Draw()
        {
            using (ImRaii.Disabled(ModCreationWindow.emotes[0] == this))
            {
                if (ImGui.Button("Remove Emote"))
                {
                    ModCreationWindow.emotes.Remove(this);
                }
            }

            ImGui.Spacing();

            ImGui.BeginGroup();
            ImGui.Text("Emote Type:");
            ImGui.SameLine(120);
            var currentModType = Enum.GetName(currentType);
            ImGui.SetNextItemWidth(120);
            using (var combo = ImRaii.Combo("##EmoteType", currentModType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(EmoteType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == currentModType))
                        {
                            currentType = Enum.Parse<EmoteType>(type);
                        }
                    }
                }
            }
            ImGui.Text("Emote Command:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(120);
            ImGui.InputText("##EmoteCommandInput", ref Command, 100);
            ImGui.Spacing();
            ImGui.Text("Animation ID:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.InputInt("##ReplacedAnimID", ref animID, 0);
            ImGui.SameLine();
            if (BearGUI.FontButton("emotesearch", FontAwesomeIcon.SearchLocation.ToIconString()))
            {
                ImGui.OpenPopup("EmoteSearch");
            }
            ImGui.SameLine();

            CalculateAnimData();

            bool animHasError = correlatedAnimName.IsNullOrEmpty() || !animHasAnyOverride;

            if (animHasError)
                BearGUI.FontText(FontAwesomeIcon.ExclamationTriangle.ToIconString(), 1.25f, 0xFF80FFFF);
            else
                BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);

            if (ImGui.IsItemHovered())
            {
                //ImGui.SetTooltip("ID of animation the pap temporarily replaces");
                using var tooltip = ImRaii.Tooltip();

                ImGui.Text("ID of animation the pap temporarily replaces");
                ImGui.Separator();
                DisplayAnimData();
            }
            ImGui.Spacing();
            ImGui.Text("Force Loop");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.Checkbox("##LoopEmote", ref isLooping);
            ImGui.Spacing();
            ImGui.Text("Hide Weapons");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.Checkbox("##HideWeapon", ref hideWeapon);
            ImGui.Spacing();
            ImGui.Text("CM Callslist:");
            ImGui.SameLine(120);
            ImGui.SetNextItemWidth(100);
            ImGui.InputText("##TracklistInput", ref tracklistPath, 500);
            ImGui.SameLine();
            if (ImGui.Button("Browse##.ivtlfile"))
            {
                WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import Tracklist File", ".ivtl", (Confirm, FilePath) =>
                {
                    if (!Confirm) return;

                    tracklistPath = FilePath;
                });
            }
            ImGui.Spacing();

            ImGui.EndGroup();

            ImGui.SameLine(300);

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 25);

            using (var popup = ImRaii.Popup("EmoteSearch"))
            {
                if (popup.Success)
                {
                    ImGui.Text("Search:");
                    ImGui.SameLine();

                    ImGui.SetNextItemWidth(150);
                    ImGui.InputText("##EmoteSearch", ref EmoteSearch, 1000);

                    var emoteList = GameResourceManager.Instance.Emotes.Select(x => x.Value).Where(x => (x.Name.RawString.Contains(EmoteSearch, StringComparison.OrdinalIgnoreCase)) && x.Name != "").ToList();

                    using (var listbox = ImRaii.ListBox($"###listbox", new(250, 200)))
                    {
                        if (listbox.Success)
                        {
                            var i = 0;
                            foreach (var emote in emoteList)
                            {
                                for (var x = 0; x < 7; x++)
                                {
                                    var currentEmote = emote.ActionTimeline[x];

                                    if (currentEmote.Value == null) continue;

                                    if (currentEmote.Value.RowId == 0) continue;

                                    i++;
                                    using (ImRaii.PushId(i))
                                    {
                                        var startPos = ImGui.GetCursorPos();

                                        var selected = ImGui.Selectable("###Selector", false, ImGuiSelectableFlags.None, new(0, 50));

                                        var endPos = ImGui.GetCursorPos();

                                        if (ImGui.IsItemVisible())
                                        {
                                            var icon = DalamudServices.textureProvider.GetFromGameIcon(new(emote.Icon)).GetWrapOrDefault();

                                            if (icon == null) continue;

                                            ImGui.SetCursorPos(new(startPos.X, startPos.Y + 5));

                                            ImGui.Image(icon.ImGuiHandle, new(40, 40));
                                            ImGui.SameLine();

                                            ImGui.BeginGroup();

                                            ImGui.Text(emote.Name.RawString);

                                            ImGui.Text(emote.Name.RawString + $" ({animationTypes[x]})");

                                            ImGui.EndGroup();

                                            ImGui.SetCursorPos(endPos);

                                            if (selected)
                                            {
                                                animID = (int)currentEmote.Value.RowId;
                                                isLooping = currentEmote.Value.IsLoop;
                                                ImGui.CloseCurrentPopup();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (var child = ImRaii.Child("##papvfxData", new(0), true))
            {
                if (child.Success)
                {
                    if (ImGui.BeginTabBar("papvfxData"))
                    {
                        if(ImGui.BeginTabItem("Pap files"))
                        {
                            if(ImGui.Button("New Path"))
                            {
                                paths.Add(new());
                            }
                            using(var dataPaths = ImRaii.Child("##DataPaths", new(0)))
                            {
                                if(dataPaths.Success)
                                {
                                    var tempList = paths.ToList();
                                    var raceMask = (RaceCodes)0;
                                    var raceMaskDuplicate = (RaceCodes)0;

                                    for(var i = 0; i < tempList.Count; i++)
                                    {
                                        tempList[i].Draw(i, this);

                                        var dupeMask = (raceMask & tempList[i].validRaces);
                                        raceMaskDuplicate |= dupeMask;

                                        if (!tempList[i].GamePath.IsNullOrEmpty())
                                            raceMask |= tempList[i].validRaces;
                                    }

                                    if (raceMaskDuplicate != (RaceCodes)0)
                                    {
                                        ImGui.Separator();
                                        var origTextColor = ImGui.GetColorU32(ImGuiCol.Text);
                                        using var pushColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF8080FF);
                                        ImGui.Text("Some races are assigned multiple times:");

                                        var raceCodes = Enum.GetNames(typeof(RaceCodes));

                                        int displayCounter = 0;
                                        for(var i = 0; i < raceCodes.Length - 1; i++)
                                        {
                                            RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);
                                            if (raceMaskDuplicate.HasFlag(raceCode))
                                            {
                                                if (displayCounter % 9 != 0)
                                                    ImGui.SameLine();
                                                ImGui.Text(raceCodes[i]);
                                                if (ImGui.IsItemHovered())
                                                {
                                                    using var tooltip = ImRaii.Tooltip();
                                                    using var pushColor2 = ImRaii.PushColor(ImGuiCol.Text, origTextColor);
                                                    ImGui.Text($"{raceCodes[i]}: {raceCode.GetDescription()}");
                                                }
                                                ++displayCounter;
                                            }
                                        }
                                    }

                                    if (tempList.Count > 0 && raceMask != RaceCodes.all)
                                    {
                                        ImGui.Separator();
                                        var origTextColor = ImGui.GetColorU32(ImGuiCol.Text);
                                        using var pushColor = ImRaii.PushColor(ImGuiCol.Text, 0xFF80FFFF);

                                        var raceCodes = Enum.GetNames(typeof(RaceCodes));

                                        int displayCounter = 0;
                                        for(var i = 0; i < raceCodes.Length - 1; i++)
                                        {
                                            RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);

                                            // Don't warn for explicitly unwanted genders
                                            if ((ModCreationWindow.selectedCatagory == ModCatagory.Male || ModCreationWindow.selectedCatagory == ModCatagory.Gay) && i % 2 != 0)
                                                continue;
                                            if ((ModCreationWindow.selectedCatagory == ModCatagory.Female || ModCreationWindow.selectedCatagory == ModCatagory.Lesbian) && i % 2 != 1)
                                                continue;

                                            if (!raceMask.HasFlag(raceCode))
                                            {
                                                if (displayCounter == 0)
                                                    ImGui.Text("Some races are not assigned:");
                                                if (displayCounter % 9 != 0)
                                                    ImGui.SameLine();
                                                ImGui.Text(raceCodes[i]);
                                                if (ImGui.IsItemHovered())
                                                {
                                                    using var tooltip = ImRaii.Tooltip();
                                                    using var pushColor2 = ImRaii.PushColor(ImGuiCol.Text, origTextColor);
                                                    ImGui.Text($"{raceCodes[i]}: {raceCode.GetDescription()}");
                                                }
                                                ++displayCounter;
                                            }
                                        }
                                    }
                                }
                            }
                            ImGui.EndTabItem();
                        }

                        if(ImGui.BeginTabItem("Persistent VFX Files"))
                        {
                            if(ImGui.Button("Add VFX Data"))
                            {
                                vFXDatas.Add(new());
                            }

                            if (ImGui.BeginTabBar("VFXData"))
                            {
                                var tempList = vFXDatas.ToList();

                                for (var i = 0; i < tempList.Count; i++)
                                {
                                    if (ImGui.BeginTabItem($"VFXData{i}"))
                                    {
                                        tempList[i].Draw(this);
                                        ImGui.EndTabItem();
                                    }
                                }

                                ImGui.EndTabBar();
                            }

                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }
                }
            }
        }
    }

    public class DataPathsUI
    {
        public string GamePath = string.Empty;
        public string LocalPath = string.Empty;
        public RaceCodes validRaces = RaceCodes.all;
        public void Draw(int IDX, ModEmoteTab modemote = null, ModSharedResourceTab sharedEmote = null)
        {
            using (ImRaii.PushId(IDX))
            {
                ImGui.BeginGroup();
                ImGui.Text("Game Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(350);
                ImGui.InputText("##GamePathInput", ref GamePath, 500);
                ImGui.SameLine();
                if (ImGui.Button("Valid Races"))
                {
                    ImGui.OpenPopup("RaceSelection");
                }
                ImGui.Text("Local Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(350);
                //ImGui.InputText("##LocalpathInput", ref LocalPath, 500);
                ModCreationWindow.DrawTruncatedLocalPath(ref LocalPath);
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 15);

                if (ImGui.Button("Remove"))
                {
                    if (modemote != null)
                    {
                        modemote.paths.Remove(this);
                    }

                    if (sharedEmote != null)
                    {
                        sharedEmote.paths.Remove(this);
                    }
                }

                using(var popup = ImRaii.Popup("RaceSelection"))
                {
                    if (popup.Success)
                    {
                        var raceCodes = Enum.GetNames(typeof(RaceCodes));

                        for(var i = 0; i < raceCodes.Length - 1; i++)
                        {
                            if(i != 0 && i % 3 != 0)
                            {
                                ImGui.SameLine();
                            }

                            RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);


                            var tempRaces = validRaces;

                            var result = tempRaces.HasFlag(raceCode);

                            if (ImGui.Checkbox(raceCodes[i], ref result))
                            {
                                if (result) tempRaces |= Enum.Parse<RaceCodes>(raceCodes[i]);
                                else tempRaces ^= Enum.Parse<RaceCodes>(raceCodes[i]);

                                validRaces = tempRaces;
                            }

                            if (ImGui.IsItemHovered())
                            {
                                using var tooltip = ImRaii.Tooltip();
                                ImGui.Text($"{raceCodes[i]}: {raceCode.GetDescription()}");
                            }
                        }

                        if (ImGui.Button("Add All Races"))
                        {
                            validRaces = RaceCodes.all;
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Remove All Races"))
                        {
                            validRaces = 0;
                        }
                    }
                }
            }
        }
    }

    public class VFXData
    {
        public VFXType vfxType;
        public RaceCodes validRaces = RaceCodes.all;
        public List<VFXPath> vfxPaths = new List<VFXPath>() { new() };

        public void Draw(ModEmoteTab tab)
        {
            if(ImGui.Button("Remove VFX"))
            {
                tab.vFXDatas.Remove(this);
            }

            ImGui.Spacing();
            ImGui.Spacing();

            ImGui.Text("VFX Type:");
            ImGui.SameLine();
            var currentVFXType = Enum.GetName(vfxType);
            ImGui.SetNextItemWidth(100);
            using (var combo = ImRaii.Combo("##VFXType", currentVFXType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(VFXType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == currentVFXType))
                        {
                            vfxType = Enum.Parse<VFXType>(type);
                        }
                    }
                }
            }

            ImGui.SameLine();

            if(ImGui.Button("Valid Races"))
            {
                ImGui.OpenPopup("RaceSelection");
            }

            using (var popup = ImRaii.Popup("RaceSelection"))
            {
                if (popup.Success)
                {
                    var raceCodes = Enum.GetNames(typeof(RaceCodes));

                    for (var i = 0; i < raceCodes.Length - 1; i++)
                    {
                        if (i != 0 && i % 3 != 0)
                        {
                            ImGui.SameLine();
                        }

                        RaceCodes raceCode = Enum.Parse<RaceCodes>(raceCodes[i]);


                        var tempRaces = validRaces;

                        var result = tempRaces.HasFlag(raceCode);

                        if (ImGui.Checkbox(raceCodes[i], ref result))
                        {
                            if (result) tempRaces |= Enum.Parse<RaceCodes>(raceCodes[i]);
                            else tempRaces ^= Enum.Parse<RaceCodes>(raceCodes[i]);

                            validRaces = tempRaces;
                        }

                        if (ImGui.IsItemHovered())
                        {
                            using var tooltip = ImRaii.Tooltip();
                            ImGui.Text($"{raceCodes[i]}: {raceCode.GetDescription()}");
                        }
                    }

                    if (ImGui.Button("Add All Races"))
                    {
                        validRaces = RaceCodes.all;
                    }

                    ImGui.SameLine();

                    if(ImGui.Button("Remove All Races"))
                    {
                        validRaces = 0;
                    }
                }
            }

            if (ImGui.Button("New VFX Path"))
            {
                vfxPaths.Add(new());
            }
            using (var vfxDataPaths = ImRaii.Child("##VFXDataPaths", new(0)))
            {
                if (vfxDataPaths.Success)
                {
                    var tempList = vfxPaths.ToList();

                    for (var i = 0; i < tempList.Count; i++)
                    {
                        tempList[i].Draw(i, this);
                    }
                }
            }
        }
    }

    public class VFXPath
    {
        public string GamePath = string.Empty;
        public string LocalPath = string.Empty;
        public void Draw(int idx, VFXData data)
        {
            using (ImRaii.PushId(idx))
            {
                ImGui.BeginGroup();
                ImGui.Text("Game Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(350);
                ImGui.InputText("##GamePathInput", ref GamePath, 500);
                ImGui.Text("Local Path:");
                ImGui.SameLine(120);
                ImGui.SetNextItemWidth(350);
                ImGui.InputText("##LocalpathInput", ref LocalPath, 500);
                ImGui.SameLine();
                if (ImGui.Button("Browse##LocalPath"))
                {
                    WindowsManager.Instance.fileDialogForPMPManager.OpenFileDialog("Square File", ".*", (Confirm, FilePath) =>
                    {
                        if (!Confirm) return;

                        LocalPath = FilePath;
                    });
                }
                ImGui.EndGroup();

                ImGui.SameLine();

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 15);

                if (ImGui.Button("Remove"))
                {
                    data.vfxPaths.Remove(this);
                }
            }
        }

    }

    public static class TracklistGenerator
    {
        public static IVTrack[] tracks = new IVTrack[0];
        public static int selectedTrackIndex = -1;
        public static void Draw()
        {
            BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("This tab is for creating special IVMP tracklist that can call certain functions of the plugin during the animation") ;
            }
            ImGui.BeginGroup();

            using (var listBox = ImRaii.ListBox("##Tracks", new(300, 0)))
            {
                if (listBox.Success)
                {
                    for(var i = 0; i < tracks.Length; i++)
                    {
                        var track = tracks[i];

                        if(ImGui.Selectable($"Call {i}##{i}", i == selectedTrackIndex))
                        {
                            selectedTrackIndex = i;
                        }
                    }
                }
            }

            if(ImGui.Button("Add Call"))
            {
               tracks = tracks.Append(new()).ToArray();
            }

            ImGui.SameLine();

            if(ImGui.Button("Remove Selected Call"))
            {
                var trackTodelete = selectedTrackIndex;

                if(selectedTrackIndex != 0)
                    selectedTrackIndex--;

                var x =  tracks.ToList();

                x.RemoveAt(trackTodelete);

                tracks = x.ToArray();

            }

            if(ImGui.Button("Export Timeline"))
            {
                ExportTimeline();
            }

            if(ImGui.Button("Reset Fields"))
            {
                tracks = new IVTrack[0];
                selectedTrackIndex = -1;
            }

            ImGui.EndGroup();

            if (selectedTrackIndex >= tracks.Length)
                selectedTrackIndex = tracks.Length - 1;

            if (selectedTrackIndex == -1)
                return;

            ImGui.SameLine(400);

            ImGui.BeginGroup();

            var currentTrack = tracks[selectedTrackIndex];

            ImGui.Text("Track Type");

            ImGui.SameLine(90);

            var selectedTrackType = Enum.GetName(currentTrack.Type);

            ImGui.SetNextItemWidth(200);
            using (var combo = ImRaii.Combo("##tracklistType", selectedTrackType))
            {
                if (combo.Success)
                {
                    var types = Enum.GetNames(typeof(TrackType));

                    foreach (var type in types)
                    {
                        if (ImGui.Selectable(type, type == selectedTrackType))
                        {
                            tracks[selectedTrackIndex].Type = Enum.Parse<TrackType>(type);
                        }
                    }
                }
            }

            ImGui.Text("Start Frame");
            ImGui.SameLine(90);
            int setFrame = (int)currentTrack.Frame;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##trackFrame", ref setFrame, 0))
            {
                tracks[selectedTrackIndex].Frame = (uint)setFrame;
            };

            string value1Text = "Unavailable";
            string value2Text = "Unavailable";
            bool value1Disabled = true;
            bool value2Disabled = true;
            string format = "%f";
            string value1Desc = "";
            string value2Desc = "";

            switch (currentTrack.Type)
            {
                case TrackType.Expression:
                    value1Text = "Emote ID";
                    value1Disabled = false;
                    value2Disabled = true;
                    format = "%d";
                    value1Desc = "Emote ID of an expression or any additive animation";
                    break;
                case TrackType.Transparency:
                    value1Text = "Value";
                    value1Disabled = false;
                    value2Disabled = true;
                    value1Desc = "Transparency level from 0.0-1.0";
                    break;
                case TrackType.FadeIn:
                    value1Text = "FadeIn Time";
                    value1Disabled = false;
                    value2Disabled = true;
                    value1Desc = "Time (in seconds) to fade in the character";
                    break;
                case TrackType.FadeOut:
                    value1Text = "FadeOut Time";
                    value1Disabled = false;
                    value2Disabled = true;
                    value1Desc = "Time (in seconds) to fade out the character";
                    break;
                case TrackType.Scale:
                    value1Text = "Scale Value";
                    value1Disabled = false;
                    value2Disabled = true;
                    value1Desc = "Force scale on character (based on racial heights)";
                    break;
                case TrackType.Outfit:
                    value2Text = ".chara Path";
                    value1Disabled = true;
                    value2Disabled = false;
                    value2Desc = "Path to .chara file. Only applies equipment and not apperance";
                    break;
                case TrackType.ChangeTime:
                    value1Text = "Time Of Day";
                    value1Disabled = false;
                    value2Disabled = true;
                    format = "%d";
                    value1Desc = "Time of day to set between 0 and 1439";
                    break;
                case TrackType.ChangeMonth:
                    value1Text = "Day of Month";
                    value1Disabled = false;
                    value2Disabled = true;
                    format = "%d";
                    value1Desc = "What month to set between 1 and 31";
                    break;
                case TrackType.ChangeSkybox:
                    value1Text = "Skybox ID";
                    value1Disabled = false;
                    value2Disabled = true;
                    format = "%d";
                    value1Desc = "Skybox ID to be set";
                    break;
            }



            using (ImRaii.Disabled(value1Disabled))
            {
                ImGui.Text(value1Text);
                ImGui.SameLine(90);
                float setValue1 = currentTrack.Value ?? 0;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputFloat("##floatValue", ref setValue1, 0, 0, format))
                {
                    tracks[selectedTrackIndex].Value = setValue1;
                }

                ImGui.SameLine();

                BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(value1Desc);
                }
            }

            using (ImRaii.Disabled(value2Disabled))
            {
                ImGui.Text(value2Text);
                ImGui.SameLine(90);
                string setValue2 = currentTrack.sValue ?? string.Empty;
                ImGui.SetNextItemWidth(200);
                if (ImGui.InputText("##stringValue", ref setValue2, 1000))
                {
                    tracks[selectedTrackIndex].sValue = setValue2;
                }

                ImGui.SameLine();

                if(currentTrack.Type == TrackType.Outfit)
                {
                    if (ImGui.Button("Browse##trackfile"))
                    {
                        WindowsManager.Instance.fileDialogManager.OpenFileDialog("Import character file", ".chara", (Confirm, FilePath) =>
                        {
                            if (!Confirm) return;

                            tracks[selectedTrackIndex].sValue = FilePath;
                        });
                    }
                }

                ImGui.SameLine();

                BearGUI.FontText(FontAwesomeIcon.QuestionCircle.ToIconString(), 1.25f);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(value2Desc);
                }
            }



            ImGui.EndGroup();
        }

        private static void ExportTimeline()
        {
            IVTracklist tracklist = new();

            tracklist.tracks = new();

            foreach (var track in tracks)
            {
                IVTrack temptrack = new()
                {
                    Frame = track.Frame,
                    Type = track.Type,
                    Value = track.Value,
                    sValue = track.sValue
                };

                tracklist.tracks.Add(temptrack);
            }

            WindowsManager.Instance.fileDialogManager.SaveFileDialog("Select a location to Save the IVTracklist", ".ivtl", $"NeTracklist.ivtl", ".ivtl", (Confirm, FilePath) =>
            {
                if (!Confirm) return;

                File.WriteAllText(Path.GetDirectoryName(FilePath) + "\\" + Path.GetFileNameWithoutExtension(FilePath) + ".ivtl", JsonHandler.Serialize(tracklist));
            });
        }
    }


}
