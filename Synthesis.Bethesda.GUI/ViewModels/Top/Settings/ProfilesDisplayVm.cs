using System;
using System.Linq;
using System.Windows.Input;
using Autofac;
using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings
{
    public class ProfilesDisplayVm : ViewModel
    {
        public ProfileManagerVm Config { get; }

        public ICommand AddCommand { get; }

        public IObservableCollection<ProfileDisplayVm> ProfilesDisplay { get; }

        [Reactive]
        public ProfileDisplayVm? DisplayedProfile { get; set; }

        [Reactive]
        public object? DisplayObject { get; set; }

        public ProfilesDisplayVm(
            ProfileManagerVm parent,
            IProfileFactory profileFactory)
        {
            Config = parent;
            AddCommand = ReactiveCommand.Create(() =>
            {
                DisplayObject = new NewProfileVm(
                    Config,
                    profileFactory);
                DisplayedProfile = null;
            });

            ProfilesDisplay = parent.Profiles.Connect()
                .Transform(x =>
                {
                    var factory = x.Scope.Resolve<ProfileDisplayVm.Factory>();
                    return factory(this, x);
                })
                // Select the currently active profile during initial display
                .OnItemAdded(p =>
                {
                    if (DisplayedProfile == null || (p.Profile?.IsActive ?? false))
                    {
                        DisplayedProfile = p;
                    }
                })
                .ObserveOnGui()
                .ToObservableCollection(this);

            this.WhenAnyValue(x => x.DisplayedProfile)
                .NotNull()
                .Subscribe(p => DisplayObject = p)
                .DisposeWith(this);
        }

        public void SwitchToActive()
        {
            DisplayedProfile = this.ProfilesDisplay.FirstOrDefault(p => p.IsActive);
        }
    }
}
