﻿using System.Windows.Input;
using ReactiveUI;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Initialization.Git;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IPatcherStartInitializationVm
    {
        ICommand AddGitPatcherCommand { get; }
        ICommand AddSolutionPatcherCommand { get; }
        ICommand AddCliPatcherCommand { get; }
    }

    public class PatcherStartInitializationVm : IPatcherStartInitializationVm
    {
        public ICommand AddGitPatcherCommand { get; }
        public ICommand AddSolutionPatcherCommand { get; }
        public ICommand AddCliPatcherCommand { get; }

        public PatcherStartInitializationVm(
            IPatcherInitializationVm initializationVm,
            GitPatcherInitVm gitPatcherInitVm,
            SolutionPatcherInitVm solutionPatcherInitVm,
            CliPatcherInitVm cliPatcherInitVm)
        {
            AddGitPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = gitPatcherInitVm;
            });
            AddSolutionPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = solutionPatcherInitVm;
            });
            AddCliPatcherCommand = ReactiveCommand.Create(() =>
            {
                initializationVm.NewPatcher = cliPatcherInitVm;
            });
        }
    }
}