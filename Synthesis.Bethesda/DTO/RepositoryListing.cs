using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.DTO
{
    public class RepositoryListing
    {
        public PatcherListing[] Patchers { get; set; } = Array.Empty<PatcherListing>();
        public string? AvatarURL { get; set; }
        public string? User { get; set; }
        public string? Repository { get; set; }
        public int Stars { get; set; }
        public int Forks { get; set; }
    }
}
