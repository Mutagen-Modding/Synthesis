using System.IO;
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
    public INavigateTo Navigate { get; }
    public IRetrieveSaveSettings RetrieveSaveSettings { get; }
    public IPipelineSettingsPath PipelinePaths { get; }
    public IGuiSettingsPath GuiPaths { get; }

    public ProfileExporter(
        INavigateTo navigate,
        IRetrieveSaveSettings retrieveSaveSettings,
        IPipelineSettingsPath pipelinePaths,
        IGuiSettingsPath guiPaths)
    {
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
        var subDir = "Export";
        Directory.CreateDirectory(subDir);
        File.WriteAllText(
            Path.Combine(subDir, PipelinePaths.Path),
            JsonConvert.SerializeObject(pipeSettings, Formatting.Indented, Execution.Constants.JsonSettings));
        File.WriteAllText(
            Path.Combine(subDir, GuiPaths.Path),
            JsonConvert.SerializeObject(guiSettings, Formatting.Indented, Execution.Constants.JsonSettings));
        var dataDir = new DirectoryInfo("Data");
        if (dataDir.Exists)
        {
            dataDir.DeepCopy(new DirectoryInfo(Path.Combine(subDir, "Data")));
        }
        Navigate.Navigate(subDir);
    }
}