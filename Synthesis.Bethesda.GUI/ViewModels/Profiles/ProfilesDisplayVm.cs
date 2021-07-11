using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Windows.Input;
using System;
using System.Linq;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Synthesis.Bethesda.GUI.DI;
using Synthesis.Bethesda.GUI.Services;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI
{
    public class ProfilesDisplayVm : ViewModel
    {
        public ConfigurationVm Config { get; }
        private readonly ViewModel? _previous;

        public ICommand GoBackCommand { get; }
        public ICommand AddCommand { get; }

        public IObservableCollection<ProfileDisplayVm> ProfilesDisplay { get; }

        [Reactive]
        public ProfileDisplayVm? DisplayedProfile { get; set; }

        [Reactive]
        public object? DisplayObject { get; set; } = null;

        public ProfilesDisplayVm(
            ILifetimeScope scope,
            ConfigurationVm parent,
            IProfileFactory profileFactory, 
            IActivePanelControllerVm activePanelController,
            ViewModel? previousPage)
        {
            _previous = previousPage;
            Config = parent;
            GoBackCommand = ReactiveCommand.Create(() =>
            {
                activePanelController.ActivePanel = _previous;
            });
            AddCommand = ReactiveCommand.Create(() =>
            {
                DisplayObject = new NewProfileVm(
                    Config,
                    profileFactory);
                DisplayedProfile = null;
            });

            var factory = scope.Resolve<ProfileDisplayVm.Factory>();
            ProfilesDisplay = parent.Profiles.Connect()
                .Transform(x => factory(this, x))
                // Select the currently active profile during initial display
                .OnItemAdded(p =>
                {
                    if (DisplayedProfile == null || (p.Profile?.IsActive ?? false))
                    {
                        DisplayedProfile = p;
                    }
                })
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
