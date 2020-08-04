using Synthesis.Bethesda.Execution.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class CodeSnippetPatcherVM : PatcherVM
    {
        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public override bool NeedsConfiguration => false;

        [Reactive]
        public string Code { get; set; } = string.Empty;

        public override ErrorResponse CanCompleteConfiguration => ErrorResponse.Success;

        public CodeSnippetPatcherVM(ProfileVM parent, CodeSnippetPatcherSettings? settings = null)
            : base(parent, settings)
        {
            CopyInSettings(settings);
            _DisplayName = this.WhenAnyValue(x => x.Nickname)
                .Select(x =>
                {
                    if (string.IsNullOrWhiteSpace(x))
                    {
                        return "<No Name>";
                    }
                    else
                    {
                        return x;
                    }
                })
                .ToGuiProperty<string>(this, nameof(DisplayName));
        }

        public override PatcherSettings Save()
        {
            var ret = new CodeSnippetPatcherSettings();
            CopyOverSave(ret);
            ret.Code = this.Code;
            return ret;
        }

        private void CopyInSettings(CodeSnippetPatcherSettings? settings)
        {
            if (settings == null) return;
            this.Code = settings.Code;
        }
    }
}
