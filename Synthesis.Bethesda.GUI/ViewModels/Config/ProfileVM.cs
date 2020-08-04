using DynamicData;
using Mutagen.Bethesda;
using Newtonsoft.Json;
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
    public class ProfileVM : ViewModel
    {
        public ConfigurationVM Config { get; }
        public GameRelease Release { get; }

        public SourceList<PatcherVM> Patchers { get; } = new SourceList<PatcherVM>();

        public ICommand AddGithubPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddSnippetPatcherCommand { get; }

        public string ID { get; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public ProfileVM(ConfigurationVM parent, GameRelease? release = null)
        {
            ID = Guid.NewGuid().ToString();
            Config = parent;
            Release = release ?? GameRelease.Oblivion;
            AddGithubPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new GithubPatcherVM(this)));
            AddSolutionPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new SolutionPatcherVM(this)));
            AddSnippetPatcherCommand = ReactiveCommand.Create(() => SetPatcherForInitialConfiguration(new CodeSnippetPatcherVM(this)));
        }

        public ProfileVM(ConfigurationVM parent, SynthesisProfile settings)
            : this(parent)
        {
            ID = settings.ID;
            Nickname = settings.Nickname;
            Release = settings.TargetRelease;
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

        public SynthesisProfile Save()
        {
            return new SynthesisProfile()
            {
                Patchers = Patchers.Items.Select(p => p.Save()).ToList(),
                ID = ID,
                Nickname = Nickname,
                TargetRelease = Release,
            };
        }

        private void SetPatcherForInitialConfiguration(PatcherVM patcher)
        {
            if (patcher.NeedsConfiguration)
            {
                Config.NewPatcher = patcher;
            }
            else
            {
                patcher.Profile.Patchers.Add(patcher);
                Config.SelectedPatcher = patcher;
            }
        }
    }
}
