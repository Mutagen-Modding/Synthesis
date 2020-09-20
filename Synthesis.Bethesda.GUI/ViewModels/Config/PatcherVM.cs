using DynamicData;
using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Synthesis.Bethesda.Execution.Patchers;
using System.Threading;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherVM : ViewModel
    {
        public ProfileVM Profile { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsSelected;
        public bool IsSelected => _IsSelected.Value;

        public int InternalID { get; }

        [Reactive]
        public bool IsOn { get; set; } = true;

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public abstract string DisplayName { get; }

        public ICommand DeleteCommand { get; }

        public abstract ConfigurationStateVM State { get; }

        private static int NextID;

        public PatcherVM(ProfileVM parent, PatcherSettings? settings)
        {
            InternalID = Interlocked.Increment(ref NextID);

            Profile = parent;
            _IsSelected = this.WhenAnyValue(x => x.Profile.Config.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            // Set to settings
            IsOn = settings?.On ?? false;
            Nickname = settings?.Nickname ?? string.Empty;

            DeleteCommand = ReactiveCommand.Create(() =>
            {
                parent.Config.MainVM.ActiveConfirmation = new ConfirmationActionVM(
                    "Confirm",
                    $"Are you sure you want to delete {DisplayName}?",
                    Delete);
            });
        }

        public abstract PatcherSettings Save();

        protected void CopyOverSave(PatcherSettings settings)
        {
            settings.On = IsOn;
            settings.Nickname = Nickname;
        }

        public abstract PatcherRunVM ToRunner(PatchersRunVM parent);

        public virtual void Delete()
        {
            Profile.Patchers.Remove(this);
        }
    }
}
