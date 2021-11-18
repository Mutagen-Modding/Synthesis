using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel
{
    public class PatcherRenameActionVm : ViewModel, IConfirmationActionVm
    {
        private readonly ILogger _logger;
        private readonly IPatcherNameVm _nameVm;
        private readonly IPatcherExtraDataPathProvider _extraDataPathProvider;
        public ReactiveCommand<Unit, Unit> ConfirmActionCommand { get; }
        public ReactiveCommand<Unit, Unit>? DiscardActionCommand => null;
        public string Title => "Rename patcher";
        public string Description => string.Empty;
        
        [Reactive] public string Name { get; set; }

        public delegate PatcherRenameActionVm Factory();

        public PatcherRenameActionVm(
            ILogger logger,
            IPatcherNameVm nameVm,
            IPatcherExtraDataPathProvider extraDataPathProvider,
            IProfileGroupsList groupsList)
        {
            _logger = logger;
            _nameVm = nameVm;
            _extraDataPathProvider = extraDataPathProvider;
            Name = nameVm.Name;
            var names = groupsList.Groups.Items
                .SelectMany(g => g.Patchers.Items)
                .Select(x => x.NameVm.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            names.Remove(Name);
            ConfirmActionCommand = ReactiveCommand.Create(
                Execute,
                this.WhenAnyValue(x => x.Name).Select(n => !names.Contains(n)));
        }

        private void Execute()
        {
            try
            {
                Directory.Move(
                    _extraDataPathProvider.Path,
                    _extraDataPathProvider.GetPathForName(Name));
            }
            catch (Exception e)
            {
                _logger.Error("Could not move settings to new nickname.  Backing out.");
                return;
            }
            _nameVm.Nickname = Name;
        }
    }
}