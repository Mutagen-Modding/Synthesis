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
        private readonly ISettingsSingleton _settingsSingleton;
        private readonly IOpenIde _openIde;
        private readonly ILogger _logger;
        private readonly IPatcherInitializationVm _init;

        public ExistingSolutionInitVm ExistingSolution { get; }
        public NewSolutionInitVm New { get; }
        public ExistingProjectInitVm ExistingProject { get; }
        
        public ICommand CompleteConfiguration => _init.CompleteConfiguration;
        public ICommand CancelConfiguration => _init.CancelConfiguration;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _canCompleteConfiguration;
        public ErrorResponse CanCompleteConfiguration => _canCompleteConfiguration.Value;

        [Reactive]
        public int SelectedIndex { get; set; }

        private readonly ObservableAsPropertyHelper<ASolutionInitializer.InitializerCall?> _targetSolutionInitializer;
        public ASolutionInitializer.InitializerCall? TargetSolutionInitializer => _targetSolutionInitializer.Value;

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
            _settingsSingleton = settingsSingleton;
            _openIde = openIde;
            _logger = logger;
            _init = init;
            OpenCodeAfter = _settingsSingleton.Gui.OpenIdeAfterCreating;
            New.ParentDirPath.TargetPath = _settingsSingleton.Gui.MainRepositoryFolder;
            Ide = _settingsSingleton.Gui.Ide;

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
            _targetSolutionInitializer = initializer
                .Select(x => x.Succeeded ? x.Value : default(ASolutionInitializer.InitializerCall?))
                .ToGuiProperty(this, nameof(TargetSolutionInitializer), default);
            _canCompleteConfiguration = initializer
                .Select(x => (ErrorResponse)x)
                .ToGuiProperty<ErrorResponse>(this, nameof(CanCompleteConfiguration), ErrorResponse.Failure);
        }

        public override void Dispose()
        {
            base.Dispose();
            _settingsSingleton.Gui.OpenIdeAfterCreating = OpenCodeAfter;
            _settingsSingleton.Gui.MainRepositoryFolder = New.ParentDirPath.TargetPath;
            _settingsSingleton.Gui.Ide = Ide;
        }

        public async IAsyncEnumerable<PatcherVm> Construct()
        {
            if (TargetSolutionInitializer == null) yield break;
            var ret = (await TargetSolutionInitializer().ConfigureAwait(false)).ToList();
            foreach (var item in ret)
            {
                yield return item;
            }

            if (OpenCodeAfter && ret.Count > 0)
            {
                try
                {
                    _openIde.OpenSolution(ret[0].SolutionPathInput.Picker.TargetPath, Ide);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error opening IDE");
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
