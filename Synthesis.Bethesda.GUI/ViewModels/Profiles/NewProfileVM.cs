using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.IO;
using System.Linq;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI
{
    public class NewProfileVM : ViewModel
    {
        private ConfigurationVM _config;
        private readonly IProfileFactory _ProfileFactory;

        public ObservableCollectionExtended<GameRelease> ReleaseOptions { get; } = new();

        [Reactive]
        public GameRelease? SelectedGame { get; set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public NewProfileVM(
            ConfigurationVM config, 
            IProfileFactory profileFactory,
            Action<ProfileVM>? postRun = null)
        {
            _config = config;
            _ProfileFactory = profileFactory;
            ReleaseOptions.AddRange(EnumExt.GetValues<GameRelease>()
                .Where(x =>
                {
                    switch (x)
                    {
                        case GameRelease.EnderalLE:
                        case GameRelease.EnderalSE:
                        case GameRelease.Fallout4:
                            return false;
                        default:
                            return true;
                    }
                }));

            var init = Inject.Container.GetInstance<PatcherInitializationVM>();
            this.WhenAnyValue(x => x.SelectedGame)
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
