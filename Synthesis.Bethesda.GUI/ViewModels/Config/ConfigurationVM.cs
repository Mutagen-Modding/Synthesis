using DynamicData;
using DynamicData.Binding;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationVM : ViewModel
    {
        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();
        public IObservableCollection<PatcherVM> PatchersDisplay { get; }

        public ICommand AddGithubPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddSnippetPatcherCommand { get; }

        [Reactive]
        public PatcherVM? SelectedPatcher { get; set; }

        public ConfigurationVM(MainVM parent)
        {
            PatchersDisplay = Patchers.Connect().ToObservableCollection(this);
            AddGithubPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new GithubPatcherVM(this)));
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new SolutionPatcherVM(this)));
            AddSnippetPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new CodeSnippetPatcherVM(this)));
        }

        public void Load(SynthesisSettings? settings)
        {
            if (settings == null) return;
            Patchers.AddRange(settings.Patchers.Select<PatcherSettings, PatcherVM>(p =>
            {
                return p switch
                {
                    GithubPatcherSettings gitHub => new GithubPatcherVM(this, gitHub),
                    CodeSnippetPatcherSettings snippet => new CodeSnippetPatcherVM(this, snippet),
                    SolutionPatcherSettings soln => new SolutionPatcherVM(this, soln),
                    _ => throw new NotImplementedException(),
                };
            }));
        }

        public SynthesisSettings Save()
        {
            return new SynthesisSettings()
            {
                Patchers = Patchers.Items.Select(p => p.Save()).ToList()
            };
        }

        private void SetPatcherForInitialConfiguration(PatcherVM patcher)
        {
            if (patcher.NeedsConfiguration)
            {
                patcher.InInitialConfiguration = true;
            }
            else
            {
                Patchers.Add(patcher);
            }
            SelectedPatcher = patcher;
        }
    }
}
