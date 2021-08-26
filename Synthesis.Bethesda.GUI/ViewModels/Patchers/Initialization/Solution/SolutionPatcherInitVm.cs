using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Initialization.Solution
{
    public class SolutionPatcherInitVm : ViewModel, IPatcherInitVm
    {
        public IShowHelpSetting ShowHelpSetting { get; }
        private readonly ISettingsSingleton _SettingsSingleton;
        private readonly IOpenIde _OpenIde;
        private readonly ILogger _Logger;
        private readonly IPatcherInitializationVm _init;

        public ExistingSolutionInitVm ExistingSolution { get; }
        public NewSolutionInitVm New { get; }
        public ExistingProjectInitVm ExistingProject { get; }
        
        public ICommand CompleteConfiguration => _init.CompleteConfiguration;
        public ICommand CancelConfiguration => _init.CancelConfiguration;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        [Reactive]
        public int SelectedIndex { get; set; }

        private readonly ObservableAsPropertyHelper<ASolutionInitializer.InitializerCall?> _TargetSolutionInitializer;
        public ASolutionInitializer.InitializerCall? TargetSolutionInitializer => _TargetSolutionInitializer.Value;

        [Reactive]
        public bool OpenCodeAfter { get; set; }

        public ObservableCollectionExtended<IDE> IdeOptions { get; } = new();
        
        [Reactive]
        public IDE Ide { get; set; }

        public SolutionPatcherInitVm(
            IShowHelpSetting showHelpSetting,
            ISettingsSingleton settingsSingleton,
            IOpenIde openIde,
            ILogger logger,
            ExistingSolutionInitVm existingSolutionInit,
            NewSolutionInitVm newSolutionInit,
            ExistingProjectInitVm existingProjectInit,
            IPatcherInitializationVm init)
        {
            ShowHelpSetting = showHelpSetting;
            IdeOptions.AddRange(EnumExt.GetValues<IDE>());
            ExistingSolution = existingSolutionInit;
            ExistingProject = existingProjectInit;
            New = newSolutionInit;
            _SettingsSingleton = settingsSingleton;
            _OpenIde = openIde;
            _Logger = logger;
            _init = init;
            OpenCodeAfter = _SettingsSingleton.Gui.OpenIdeAfterCreating;
            New.ParentDirPath.TargetPath = _SettingsSingleton.Gui.MainRepositoryFolder;
            Ide = _SettingsSingleton.Gui.Ide;

            var initializer = this.WhenAnyValue(x => x.SelectedIndex)
                .Select<int, ASolutionInitializer>(x =>
                {
                    return ((SolutionInitType)x) switch
                    {
                        SolutionInitType.ExistingSolution => ExistingSolution,
                        SolutionInitType.New => New,
                        SolutionInitType.ExistingProject => ExistingProject,
                        _ => throw new NotImplementedException(),
                    };
                })
                .Select(x => x.InitializationCall)
                .Switch()
                .Replay(1)
                .RefCount();
            _TargetSolutionInitializer = initializer
                .Select(x => x.Succeeded ? x.Value : default(ASolutionInitializer.InitializerCall?))
                .ToGuiProperty(this, nameof(TargetSolutionInitializer), default);
            _CanCompleteConfiguration = initializer
                .Select(x => (ErrorResponse)x)
                .ToGuiProperty<ErrorResponse>(this, nameof(CanCompleteConfiguration), ErrorResponse.Failure);
        }

        public override void Dispose()
        {
            base.Dispose();
            _SettingsSingleton.Gui.OpenIdeAfterCreating = OpenCodeAfter;
            _SettingsSingleton.Gui.MainRepositoryFolder = New.ParentDirPath.TargetPath;
            _SettingsSingleton.Gui.Ide = Ide;
        }

        public async IAsyncEnumerable<PatcherVm> Construct()
        {
            if (TargetSolutionInitializer == null) yield break;
            var ret = (await TargetSolutionInitializer()).ToList();
            foreach (var item in ret)
            {
                yield return item;
            }

            if (OpenCodeAfter && ret.Count > 0)
            {
                try
                {
                    _OpenIde.OpenSolution(ret[0].SolutionPathInput.Picker.TargetPath, Ide);
                }
                catch (Exception ex)
                {
                    _Logger.Error(ex, "Error opening IDE");
                }
            }
        }

        public void Cancel()
        {
        }

        public enum SolutionInitType
        {
            New,
            ExistingSolution,
            ExistingProject,
        }
    }
}
