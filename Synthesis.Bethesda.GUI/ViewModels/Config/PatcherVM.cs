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
using System.Threading;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Mutagen.Bethesda;

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

        public ICommand GoToErrorCommand => NoggogCommand.CreateFromObject(
            objectSource: this.WhenAnyValue(x => x.State.RunnableState),
            canExecute: x => x.Failed,
            execute: x => DisplayedObject = ErrorVM,
            disposable: this.CompositeDisposable);

        public abstract ConfigurationState State { get; }

        [Reactive]
        public ViewModel DisplayedObject { get; set; }

        private static int NextID;

        public ErrorVM ErrorVM { get; }

        public virtual bool IsNameEditable => true;

        public PatcherVM(ProfileVM parent, PatcherSettings? settings)
        {
            DisplayedObject = this;
            InternalID = Interlocked.Increment(ref NextID);
            ErrorVM = new ErrorVM("Error", backAction: () =>
            {
                DisplayedObject = this;
            });

            Profile = parent;
            _IsSelected = this.WhenAnyValue(x => x.Profile.SelectedPatcher)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            // Set to settings
            IsOn = settings?.On ?? false;
            Nickname = settings?.Nickname ?? string.Empty;

            DeleteCommand = ReactiveCommand.Create(() =>
            {
                parent.Config.MainVM.TargetConfirmation = new ConfirmationActionVM(
                    "Confirm",
                    $"Are you sure you want to delete {DisplayName}?",
                    Delete);
            });

            this.WhenAnyValue(x => x.IsSelected)
                .DistinctUntilChanged()
                .Where(x => x)
                .Subscribe(_ =>
                {
                    DisplayedObject = this;
                })
                .DisposeWith(this);

            this.WhenAnyValue(x => x.State.RunnableState)
                .Subscribe(state =>
                {
                    if (state.Failed)
                    {
                        ErrorVM.String = state.Reason;
                    }
                    else
                    {
                        ErrorVM.String = null;
                    }
                })
                .DisposeWith(this);
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

        protected ILogger Logger => Log.Logger.ForContext(nameof(DisplayName), DisplayName);
    }
}
