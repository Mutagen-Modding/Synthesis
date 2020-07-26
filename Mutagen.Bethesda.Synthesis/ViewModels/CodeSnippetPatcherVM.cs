using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class CodeSnippetPatcherVM : PatcherVM
    {
        public CodeSnippetPatcherVM(MainVM mvm)
            : base(mvm)
        {
        }

        public CodeSnippetPatcherVM(MainVM mvm, SnippetPatcherSettings settings)
            : this(mvm)
        {
        }

        public override PatcherSettings Save()
        {
            return new SnippetPatcherSettings();
        }
    }
}
