using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    public class RepositoryListing
    {
        public PatcherListing[] Patchers { get; set; } = Array.Empty<PatcherListing>();
        public string? AvatarURL { get; set; }
        public string User { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public int Stars { get; set; }
        public int Forks { get; set; }
    }
}
