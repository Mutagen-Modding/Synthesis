using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class SolutionPatcherInitVM : PatcherInitVM
    {
        public ExistingSolutionInitVM Existing { get; } = new ExistingSolutionInitVM();
        public NewSolutionInitVM New { get; } = new NewSolutionInitVM();

        private readonly SolutionPatcherVM _patcher;
        public override PatcherVM Patcher => _patcher;

        private readonly ObservableAsPropertyHelper<ErrorResponse> _CanCompleteConfiguration;
        public override ErrorResponse CanCompleteConfiguration => _CanCompleteConfiguration.Value;

        [Reactive]
        public int SelectedIndex { get; set; }

        private readonly ObservableAsPropertyHelper<Func<SolutionPatcherVM, Task>?> _TargetSolutionInitializer;
        public Func<SolutionPatcherVM, Task>? TargetSolutionInitializer => _TargetSolutionInitializer.Value;

        public SolutionPatcherInitVM(SolutionPatcherVM patcher)
        {
            _patcher = patcher;

            var initializer = this.WhenAnyValue(x => x.SelectedIndex)
                .Select<int, ASolutionInitializer>(x =>
                {
                    return ((SolutionInitType)x) switch
                    {
                        SolutionInitType.Existing => Existing,
                        SolutionInitType.New => New,
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
        }

        public enum SolutionInitType
        {
            New,
            Existing
        }
    }
}
