using Synthesis.Bethesda.Execution.Settings;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Threading;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI
{
    public abstract class PatcherVM : ViewModel
    {
        private readonly IRemovePatcherFromProfile _Remove;

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

        public PatcherVM(
            IRemovePatcherFromProfile remove,
            IProfileDisplayControllerVm selPatcher,
            IConfirmationPanelControllerVm confirmation,
            PatcherSettings? settings)
        {
            _Remove = remove;
            DisplayedObject = this;
            InternalID = Interlocked.Increment(ref NextID);
            ErrorVM = new ErrorVM("Error", backAction: () =>
            {
                DisplayedObject = this;
            });

            _IsSelected = selPatcher.WhenAnyValue(x => x.SelectedObject)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsSelected));

            // Set to settings
            IsOn = settings?.On ?? false;
            Nickname = settings?.Nickname ?? string.Empty;

            DeleteCommand = ReactiveCommand.Create(() =>
            {
                confirmation.TargetConfirmation = new ConfirmationActionVM(
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
            _Remove.Remove(this);
        }

        protected ILogger Logger => Log.Logger.ForContext(nameof(DisplayName), DisplayName);

        public virtual void SuccessfulRunCompleted()
        {
        }
    }
}
