using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Design;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using DynamicData.Binding;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherInitVM : PatcherInitVM
    {
        public MainVM MVM { get; }

        public ExistingSolutionInitVM ExistingSolution { get; } = new ExistingSolutionInitVM();
        public NewSolutionInitVM New { get; } = new NewSolutionInitVM();
        public ExistingProjectInitVM ExistingProject { get; } = new ExistingProjectInitVM();

        private readonly SolutionPatcherVM _patcher;
        public override PatcherVM Patcher => _patcher;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        [Reactive]
        public int SelectedIndex { get; set; }

        private readonly ObservableAsPropertyHelper<Func<SolutionPatcherVM, Task>?> _TargetSolutionInitializer;
        public Func<SolutionPatcherVM, Task>? TargetSolutionInitializer => _TargetSolutionInitializer.Value;

        [Reactive]
        public bool OpenCodeAfter { get; set; }

        public SolutionPatcherInitVM(MainVM mvm, SolutionPatcherVM patcher)
        {
            MVM = mvm;
            _patcher = patcher;
            OpenCodeAfter = _patcher.Profile.Config.MainVM.Settings.OpenIdeAfterCreating;
            New.ParentDirPath.TargetPath = _patcher.Profile.Config.MainVM.Settings.MainRepositoryFolder;

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
                .Select(x => x.Succeeded ? x.Value : default(Func<SolutionPatcherVM, Task>?))
                .ToGuiProperty(this, nameof(TargetSolutionInitializer));
            _CanCompleteConfiguration = initializer
                .Select(x => (ErrorResponse)x)
                .ToGuiProperty<ErrorResponse>(this, nameof(CanCompleteConfiguration), ErrorResponse.Failure);
        }

        public override async Task ExecuteChanges()
        {
            if (TargetSolutionInitializer == null) return;
            await TargetSolutionInitializer(_patcher);
            try
            {
                IdeLocator.OpenSolution(_patcher.SolutionPath.TargetPath, MVM.Ide);
            }
            catch (Exception)
            {
                //TODO
                //log
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _patcher.Profile.Config.MainVM.Settings.OpenIdeAfterCreating = OpenCodeAfter;
            _patcher.Profile.Config.MainVM.Settings.MainRepositoryFolder = New.ParentDirPath.TargetPath;
        }

        public enum SolutionInitType
        {
            New,
            ExistingSolution,
            ExistingProject,
        }
    }
}
