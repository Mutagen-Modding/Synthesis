using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI
{
    public class NewProfileVM : ViewModel
    {
        private ConfigurationVM _config;

        public ObservableCollectionExtended<GameRelease> ReleaseOptions { get; } = new();

        [Reactive]
        public GameRelease? SelectedGame { get; set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public NewProfileVM(ConfigurationVM config, Action<ProfileVM> postRun)
        {
            _config = config;
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

            this.WhenAnyValue(x => x.SelectedGame)
                .Subscribe(game =>
                {
                    if (game == null) return;
                    var profile = new ProfileVM(config, game.Value, GetNewProfileId(),
                        Inject.Scope.GetRequiredService<INavigateTo>())
                    {
                        Nickname = Nickname
                    };
                    config.Profiles.AddOrUpdate(profile);
                    postRun(profile);
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
