using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileDisplayVM : ViewModel
    {
        public ProfilesDisplayVM Parent { get; }

        public ProfileVM Profile { get; }

        private readonly ObservableAsPropertyHelper<bool> _IsDisplaying;
        public bool IsDisplaying => _IsDisplaying.Value;

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        public ICommand DeleteCommand { get; }
        public ICommand SwitchToCommand { get; }
        public ICommand OpenInternalProfileFolderCommand { get; }

        public ObservableCollectionExtended<PersistenceMode> PersistenceModes { get; } = new(EnumExt.GetValues<PersistenceMode>());

        public delegate ProfileDisplayVM Factory(ProfilesDisplayVM parent, ProfileVM profile);
        
        public ProfileDisplayVM(
            ProfilesDisplayVM parent,
            INavigateTo navigate, 
            ISelectedProfileControllerVm selProfile,
            IConfirmationPanelControllerVm confirmation,
            ProfileVM profile)
        {
            Parent = parent;
            Profile = profile;

            _IsDisplaying = parent.WhenAnyValue(x => x.DisplayedProfile)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsDisplaying));

            _IsActive = this.WhenAnyValue(x => x.Profile.IsActive)
                .ToGuiProperty(this, nameof(IsActive));

            DeleteCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.Profile!.IsActive)
                    .Select(active => !active),
                execute: () =>
                {
                    var profile = this.Profile;
                    if (profile.IsActive) return;
                    confirmation.TargetConfirmation = new ConfirmationActionVM(
                        "Confirm",
                        $"Are you sure you want to delete {profile.Nickname}?",
                        () =>
                        {
                            parent.Config.Profiles.Remove(profile);
                            Parent.SwitchToActive();
                        });
                });
            SwitchToCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    var profile = this.Profile;
                    if (profile.IsActive) return;
                    selProfile.SelectedProfile = profile;
                });
            OpenInternalProfileFolderCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    navigate.Navigate(Profile!.ProfileDirectory);
                },
                canExecute: this.WhenAnyValue(x => x.Profile)
                    .Select(x => x != null));
        }
    }
}
