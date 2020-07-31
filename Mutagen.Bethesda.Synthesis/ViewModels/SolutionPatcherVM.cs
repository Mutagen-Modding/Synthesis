using DynamicData;
using Microsoft.WindowsAPICodePack.Dialogs;
using Mutagen.Bethesda.Synthesis.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class SolutionPatcherVM : PatcherVM
    {
        public PathPickerVM SolutionPath { get; } = new PathPickerVM()
        {
            ExistCheckOption = PathPickerVM.CheckOptions.On,
            PathType = PathPickerVM.PathTypeOptions.File,
        };

        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public override bool NeedsConfiguration => true;

        protected override IObservable<ErrorResponse> CanCompleteConfiguration => this.WhenAnyValue(x => x.SolutionPath.ErrorState);

        public SolutionPatcherVM(MainVM mvm, SolutionPatcherSettings? settings = null)
            : base(mvm, settings)
        {
            SolutionPath.Filters.Add(new CommonFileDialogFilter("Solution", ".sln"));
            _DisplayName = this.WhenAnyValue(
                x => x.Nickname,
                x => x.SolutionPath.TargetPath,
                (nickname, path) =>
                {
                    if (!string.IsNullOrWhiteSpace(nickname)) return nickname;
                    try
                    {
                        var name = Path.GetFileName(Path.GetDirectoryName(path));
                        if (string.IsNullOrWhiteSpace(name)) return "Mutagen Solution Patcher";
                        return name;
                    }
                    catch (Exception)
                    {
                        return "Mutagen Solution Patcher";
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));
        }

        public override PatcherSettings Save()
        {
            var ret = new SolutionPatcherSettings();
            CopyOverSave(ret);
            return ret;
        }
    }
}
