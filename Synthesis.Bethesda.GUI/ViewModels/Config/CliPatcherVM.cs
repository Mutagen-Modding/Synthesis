using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class CliPatcherVM : PatcherVM
    {
        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public readonly PathPickerVM PathToExecutable = new PathPickerVM()
        {
             PathType = PathPickerVM.PathTypeOptions.File,
             ExistCheckOption = PathPickerVM.CheckOptions.On,
        };

        private readonly ObservableAsPropertyHelper<ConfigurationStateVM> _State;
        public override ConfigurationStateVM State => _State.Value;

        public CliPatcherVM(ProfileVM parent, CliPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            _DisplayName = this.WhenAnyValue(
                    x => x.Nickname,
                    x => x.PathToExecutable.TargetPath,
                    (Nickname, PathToExecutable) => (Nickname, PathToExecutable))
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x.Nickname))
                    {
                        try
                        {
                            return Path.GetFileNameWithoutExtension(x.PathToExecutable);
                        }
                        catch (Exception)
                        {
                            return "<Naming Error>";
                        }
                    }
                    else
                    {
                        return x.Nickname;
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));

            _State = this.WhenAnyValue(x => x.PathToExecutable.ErrorState)
                .Select(e =>
                {
                    return new ConfigurationStateVM()
                    {
                        IsHaltingError = !e.Succeeded,
                        RunnableState = e
                    };
                })
                .ToGuiProperty<ConfigurationStateVM>(this, nameof(State), ConfigurationStateVM.Success);
        }
        private void CopyInSettings(CliPatcherSettings? settings)
        {
            if (settings == null) return;
            PathToExecutable.TargetPath = settings.PathToExecutable;
        }

        public override PatcherSettings Save()
        {
            var ret = new CliPatcherSettings();
            CopyOverSave(ret);
            ret.PathToExecutable = PathToExecutable.TargetPath;
            return ret;
        }

        public override PatcherRunVM ToRunner(PatchersRunVM parent)
        {
            return new PatcherRunVM(
                parent, 
                this, 
                new CliPatcherRun(nickname: DisplayName, pathToExecutable: PathToExecutable.TargetPath));
        }

        public override PatcherInitVM? CreateInitializer()
        {
            return new CliPatcherInitVM(this);
        }
    }
}
