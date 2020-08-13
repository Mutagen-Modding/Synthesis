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
    public class NewProfileVM : ViewModel
    {
        public ObservableCollectionExtended<GameRelease> ReleaseOptions { get; } = new ObservableCollectionExtended<GameRelease>();

        [Reactive]
        public GameRelease? SelectedGame { get; set; }

        [Reactive]
        public string Nickname { get; set; } = string.Empty;

        public NewProfileVM(ConfigurationVM config, Action<ProfileVM> postRun)
        {
            ReleaseOptions.AddRange(EnumExt.GetValues<GameRelease>());

            this.WhenAnyValue(x => x.SelectedGame)
                .Subscribe(game =>
                {
                    if (game == null) return;
                    var profile = new ProfileVM(config, game.Value)
                    {
                        Nickname = Nickname
                    };
                    config.Profiles.AddOrUpdate(profile);
                    postRun(profile);
                })
                .DisposeWith(this);
        }
    }
}
