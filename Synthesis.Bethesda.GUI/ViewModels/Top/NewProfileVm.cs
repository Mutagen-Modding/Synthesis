using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.ViewModels.Profiles;

namespace Synthesis.Bethesda.GUI.ViewModels.Top
{
    public class NewProfileVm : ViewModel
    {
        private ConfigurationVm _config;
        private readonly IProfileFactory _ProfileFactory;

        public ObservableCollectionExtended<GameCategory> CategoryOptions { get; } = new();

        public IObservableCollection<GameRelease> ReleaseOptions { get; }

        [Reactive]
        public GameCategory? SelectedCategory { get; set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        [Reactive]
        public GameRelease? SelectedRelease { get; set; }

        public NewProfileVm(
            ConfigurationVm config, 
            IProfileFactory profileFactory,
            Action<ProfileVm>? postRun = null)
        {
            _config = config;
            _ProfileFactory = profileFactory;
            CategoryOptions.AddRange(EnumExt.GetValues<GameCategory>());

            ReleaseOptions = this.WhenAnyValue(x => x.SelectedCategory)
                .Select(x => x?.GetRelatedReleases().AsObservableChangeSet() ?? Observable.Return(ChangeSet<GameRelease>.Empty))
                .Switch()
                .ToObservableCollection(this);

            this.WhenAnyValue(x => x.SelectedCategory)
                .NotNull()
                .Select(x => x.GetRelatedReleases())
                .Where(x => x.Count() == 1)
                .Subscribe(rel =>
                {
                    SelectedRelease = rel.FirstOrDefault();
                })
                .DisposeWith(this);

            this.WhenAnyValue(x => x.SelectedRelease)
                .Subscribe(game =>
                {
                    if (game == null) return;
                    var profile = _ProfileFactory.Get(game.Value, GetNewProfileId(), Nickname.IsNullOrWhitespace() ? game.Value.ToDescriptionString() : Nickname);
                    config.Profiles.AddOrUpdate(profile);
                    postRun?.Invoke(profile);
                })
                .DisposeWith(this);
        }

        public string GetNewProfileId()
        {
            bool IsValid(string id)
            {
                foreach (var profile in _config.Profiles.Items)
                {
                    if (profile.ID == id)
                    {
                        return false;
                    }
                }
                return true;
            }

            for (int i = 0; i < 15; i++)
            {
                var attempt = Path.GetRandomFileName();
                if (IsValid(attempt))
                {
                    return attempt;
                }
            }

            throw new ArgumentException("Could not allocate a new profile");
        }
    }
}
