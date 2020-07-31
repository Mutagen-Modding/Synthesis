using Mutagen.Bethesda.Synthesis.Settings;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class CodeSnippetPatcherVM : PatcherVM
    {
        private readonly ObservableAsPropertyHelper<string> _DisplayName;
        public override string DisplayName => _DisplayName.Value;

        public override bool NeedsConfiguration => false;

        public CodeSnippetPatcherVM(MainVM mvm, SnippetPatcherSettings? settings = null)
            : base(mvm, settings)
        {
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
            var ret = new SnippetPatcherSettings();
            CopyOverSave(ret);
            return ret;
        }
    }
}
