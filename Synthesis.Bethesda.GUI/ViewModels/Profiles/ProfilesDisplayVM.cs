﻿using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Windows.Input;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI
{
    public class ProfilesDisplayVM : ViewModel
    {
        public ConfigurationVM Config { get; }
        private readonly ViewModel? _previous;

        public ICommand GoBackCommand { get; }
        public ICommand AddCommand { get; }

        public IObservableCollection<ProfileDisplayVM> ProfilesDisplay { get; }

        [Reactive]
        public ProfileDisplayVM? DisplayedProfile { get; set; }

        [Reactive]
        public object? DisplayObject { get; set; } = null;

        public ProfilesDisplayVM(ConfigurationVM parent, ViewModel? previousPage)
        {
            _previous = previousPage;
            Config = parent;
            var activePanelController = Inject.Container.GetInstance<IActivePanelControllerVm>();
            GoBackCommand = ReactiveCommand.Create(() =>
            {
                activePanelController.ActivePanel = _previous;
            });
            AddCommand = ReactiveCommand.Create(() =>
            {
                DisplayObject = new NewProfileVM(
                    Config,
                    Inject.Container.GetInstance<IProfileFactory>());
                DisplayedProfile = null;
            });

            ProfilesDisplay = parent.Profiles.Connect()
                .Transform(x => new ProfileDisplayVM(this, Inject.Container.GetInstance<INavigateTo>(), x))
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
