using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;
using Synthesis.Bethesda.GUI.Services.Ide;
using Synthesis.Bethesda.GUI.Settings;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherInitVM : PatcherInitVM
    {
        public IShowHelpSetting ShowHelpSetting { get; }
        public ProfileVM Profile { get; }
        private readonly ISettingsSingleton _SettingsSingleton;
        
        public ExistingSolutionInitVM ExistingSolution { get; } = new();
        public NewSolutionInitVM New { get; } = new();
        public ExistingProjectInitVM ExistingProject { get; } = new();

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        [Reactive]
        public int SelectedIndex { get; set; }

        private readonly ObservableAsPropertyHelper<ASolutionInitializer.InitializerCall?> _TargetSolutionInitializer;
        public ASolutionInitializer.InitializerCall? TargetSolutionInitializer => _TargetSolutionInitializer.Value;

        [Reactive]
        public bool OpenCodeAfter { get; set; }

        public ObservableCollectionExtended<IDE> IdeOptions { get; } = new();
        
        [Reactive]
        public IDE Ide { get; set; }

        public SolutionPatcherInitVM(
            IShowHelpSetting showHelpSetting,
            PatcherInitializationVM init,
            ProfileVM profile)
            : base(init)
        {
            ShowHelpSetting = showHelpSetting;
            Profile = profile;
            IdeOptions.AddRange(EnumExt.GetValues<IDE>());
            _SettingsSingleton = Inject.Scope.GetInstance<ISettingsSingleton>();
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

        public override async IAsyncEnumerable<PatcherVM> Construct()
        {
            if (TargetSolutionInitializer == null) yield break;
            var ret = (await TargetSolutionInitializer(Profile)).ToList();
            foreach (var item in ret)
            {
                yield return item;
            }

            if (OpenCodeAfter && ret.Count > 0)
            {
                try
                {
                    Inject.Scope.GetInstance<IOpenIde>()
                        .OpenSolution(ret[0].SolutionPath.TargetPath, Ide);
                }
                catch (Exception ex)
                {
                    Log.Logger.Error(ex, "Error opening IDE");
                }
            }
        }

        public enum SolutionInitType
        {
            New,
            ExistingSolution,
            ExistingProject,
        }
    }
}
