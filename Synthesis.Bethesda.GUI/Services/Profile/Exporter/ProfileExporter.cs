using System.IO;
using System.IO.Abstractions;
using Newtonsoft.Json;
using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI.Services.Profile.Exporter;

public interface IProfileExporter
{
    void Export(string id);
}

public class ProfileExporter : IProfileExporter
{
    public IFileSystem FileSystem { get; }
    public INavigateTo Navigate { get; }
    public IRetrieveSaveSettings RetrieveSaveSettings { get; }
    public IPipelineSettingsPath PipelinePaths { get; }
    public IGuiSettingsPath GuiPaths { get; }

    public ProfileExporter(
        IFileSystem fileSystem,
        INavigateTo navigate,
        IRetrieveSaveSettings retrieveSaveSettings,
        IPipelineSettingsPath pipelinePaths,
        IGuiSettingsPath guiPaths)
    {
        FileSystem = fileSystem;
        Navigate = navigate;
        RetrieveSaveSettings = retrieveSaveSettings;
        PipelinePaths = pipelinePaths;
        GuiPaths = guiPaths;
    }
        
    public void Export(string id)
    {
        RetrieveSaveSettings.Retrieve(out var guiSettings, out var pipeSettings);
        pipeSettings.Profiles.RemoveWhere(p => p.ID != id);
        guiSettings.SelectedProfile = id;
        if (pipeSettings.Profiles.Count != 1)
        {
            throw new ArgumentException("Unexpected number of profiles for export");
        }
        var profile = pipeSettings.Profiles[0];
        profile.LockToCurrentVersioning = true;
        foreach (var gitPatcher in profile.Groups
                     .SelectMany(x => x.Patchers)
                     .WhereCastable<PatcherSettings, GithubPatcherSettings>())
        {
            gitPatcher.AutoUpdateToBranchTip = false;
            gitPatcher.LatestTag = false;
        }
        DirectoryPath subDir = "Export";
        if (FileSystem.Directory.Exists(subDir))
        {
            FileSystem.Directory.DeleteEntireFolder(subDir, deleteFolderItself: false);
        }
        else
        {
            FileSystem.Directory.CreateDirectory(subDir);
        }
        FileSystem.File.WriteAllText(
            Path.Combine(subDir, Path.GetFileName(PipelinePaths.Path)),
            JsonConvert.SerializeObject(pipeSettings, Formatting.Indented, Execution.Constants.JsonSettings));
        FileSystem.File.WriteAllText(
            Path.Combine(subDir, Path.GetFileName(GuiPaths.Path)),
            JsonConvert.SerializeObject(guiSettings, Formatting.Indented, Execution.Constants.JsonSettings));
        DirectoryPath dataDir = "Data";
        if (FileSystem.Directory.Exists(dataDir))
        {
            FileSystem.Directory.DeepCopy(dataDir, Path.Combine(subDir, "Data"));
        }
        Navigate.Navigate(subDir);
    }
}