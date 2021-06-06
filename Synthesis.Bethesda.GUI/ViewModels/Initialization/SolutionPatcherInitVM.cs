using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using DynamicData.Binding;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherInitVM : PatcherInitVM
    {
        private readonly ISettings _Settings;
        
        public ExistingSolutionInitVM ExistingSolution { get; } = new ExistingSolutionInitVM();
        public NewSolutionInitVM New { get; } = new NewSolutionInitVM();
        public ExistingProjectInitVM ExistingProject { get; } = new ExistingProjectInitVM();

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

        public SolutionPatcherInitVM(ProfileVM profile)
            : base(profile)
        {
            IdeOptions.AddRange(EnumExt.GetValues<IDE>());
            _Settings = Inject.Scope.GetInstance<ISettings>();
            OpenCodeAfter = _Settings.Gui.OpenIdeAfterCreating;
            New.ParentDirPath.TargetPath = _Settings.Gui.MainRepositoryFolder;
            Ide = _Settings.Gui.Ide;

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
            _Settings.Gui.OpenIdeAfterCreating = OpenCodeAfter;
            _Settings.Gui.MainRepositoryFolder = New.ParentDirPath.TargetPath;
            _Settings.Gui.Ide = Ide;
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
                    IdeLocator.OpenSolution(ret[0].SolutionPath.TargetPath, Ide);
                }
                catch (Exception)
                {
                    //TODO
                    //log
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
