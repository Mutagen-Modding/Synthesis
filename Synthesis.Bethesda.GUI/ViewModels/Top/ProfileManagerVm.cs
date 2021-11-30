using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Settings.V2;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public interface ISelectedProfileControllerVm
    {
        ProfileVm? SelectedProfile { get; set; }
    }
    
    public class ProfileManagerVm : ViewModel, ISelectedProfileControllerVm
    {
        private readonly IProfileFactory _profileFactory;

        public SourceCache<ProfileVm, string> Profiles { get; } = new(p => p.ID);

        public ReactiveCommandBase<Unit, Unit> RunPatchers { get; }

        private readonly ObservableAsPropertyHelper<ViewModel?> _displayedObject;
        public ViewModel? DisplayedObject => _displayedObject.Value;
        
        [Reactive]
        public ProfileVm? SelectedProfile { get; set; }

        public ProfileManagerVm(
            ISaveSignal saveSignal,
            IProfileFactory profileFactory,
            ILogger logger)
        {
            logger.Information("Creating ConfigurationVM");
            _profileFactory = profileFactory;

            _displayedObject = this.WhenAnyValue(x => x.SelectedProfile!.DisplayController.SelectedObject)
                .ToGuiProperty(this, nameof(DisplayedObject), default);

            RunPatchers = NoggogCommand.CreateFromObject(
                objectSource: this.WhenAnyValue(x => x.SelectedProfile),
                canExecute: (profileObs) => profileObs.Select(profile => profile.WhenAnyValue(x => x!.State))
                    .Switch()
                    .Select(err => err.Succeeded),
                execute: (profile) =>
                {
                    if (profile == null) return;
                    profile.StartRun();
                },
                disposable: this);

            saveSignal.Saving
                .Subscribe(x => Save(x.Gui, x.Pipe))
                .DisposeWith(this);
        }

        public void Load(ISynthesisGuiSettings settings, IPipelineSettings pipeSettings)
        {
            Profiles.Clear();
            Profiles.AddOrUpdate(pipeSettings.Profiles.Select(p =>
            {
                return _profileFactory.Get(p);
            }));
            if (Profiles.TryGetValue(settings.SelectedProfile, out var profile))
            {
                SelectedProfile = profile;
            }
        }

        private void Save(SynthesisGuiSettings guiSettings, PipelineSettings pipeSettings)
        {
            guiSettings.SelectedProfile = SelectedProfile?.ID ?? string.Empty;
            pipeSettings.Profiles = Profiles.Items.Select(p => p.Save()).ToList<ISynthesisProfileSettings>();
        }

        public override void Dispose()
        {
            base.Dispose();
            Profiles.Items.ForEach(p => p.Dispose());
        }
    }
}
