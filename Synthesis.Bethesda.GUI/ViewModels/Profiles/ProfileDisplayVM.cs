using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileDisplayVM : ViewModel
    {
        public ProfilesDisplayVM Parent { get; }

        [Reactive]
        public ProfileVM? Profile { get; private set; }

        private readonly ObservableAsPropertyHelper<bool> _IsDisplaying;
        public bool IsDisplaying => _IsDisplaying.Value;

        private readonly ObservableAsPropertyHelper<bool> _IsActive;
        public bool IsActive => _IsActive.Value;

        public ICommand DeleteCommand { get; }
        public ICommand SwitchToCommand { get; }
        public ICommand OpenInternalProfileFolderCommand { get; }

        public ProfileDisplayVM(ProfilesDisplayVM parent, ProfileVM? profile = null)
        {
            Parent = parent;
            Profile = profile;

            _IsDisplaying = parent.WhenAnyValue(x => x.DisplayedProfile)
                .Select(x => x == this)
                .ToGuiProperty(this, nameof(IsDisplaying));

            _IsActive = this.WhenAnyFallback(x => x.Profile!.IsActive, fallback: false)
                .ToGuiProperty(this, nameof(IsActive));

            DeleteCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyFallback(x => x.Profile!.IsActive, fallback: true)
                    .Select(active => !active),
                execute: () =>
                {
                    var profile = this.Profile;
                    if (profile == null || profile.IsActive) return;
                    Parent.Config.MainVM.ActiveConfirmation = new ConfirmationActionVM(
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
                    if (profile == null || profile.IsActive) return;
                    Parent.Config.SelectedProfile = profile;
                });
            OpenInternalProfileFolderCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    Utility.NavigateToPath(Profile!.ProfileDirectory);
                },
                canExecute: this.WhenAnyValue(x => x.Profile)
                    .Select(x => x != null));
        }
    }
}
