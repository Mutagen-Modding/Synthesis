using Mutagen.Bethesda.Synthesis.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Mutagen.Bethesda.Synthesis
{
    public class GithubPatcherVM : PatcherVM
    {
        public GithubPatcherVM(MainVM mvm)
            : base(mvm)
        {
        }

        public GithubPatcherVM(MainVM mvm, GithubPatcherSettings settings)
            : this(mvm)
        {
        }

        public override PatcherSettings Save()
        {
            return new GithubPatcherSettings();
        }
    }
}
