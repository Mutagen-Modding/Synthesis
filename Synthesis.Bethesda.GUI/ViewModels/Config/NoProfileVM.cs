using DynamicData;
using DynamicData.Binding;
using Mutagen.Bethesda;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class NoProfileVM : ViewModel
    {
        public ObservableCollectionExtended<GameRelease> ReleaseOptions { get; } = new ObservableCollectionExtended<GameRelease>();

        [Reactive]
        public GameRelease? SelectedGame { get; set; }

        public NoProfileVM(ConfigurationVM config)
        {
            ReleaseOptions.AddRange(EnumExt.GetValues<GameRelease>());

            this.WhenAnyValue(x => x.SelectedGame)
                .Subscribe(game =>
                {
                    if (game == null) return;
                    var profile = new ProfileVM(config, game.Value)
                    {
                        Nickname = "Initial Profile"
                    };
                    config.Profiles.AddOrUpdate(profile);
                    config.SelectedProfile = profile;
                    config.MainVM.ActivePanel = config;
                })
                .DisposeWith(this);
        }
    }
}
