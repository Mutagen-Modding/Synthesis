using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Synthesis.Bethesda.UnitTests
{
    public class GameReleaseTests
    {
        [Fact]
        public void ToWjGame()
        {
            foreach (var rel in EnumExt.GetValues<GameRelease>())
            {
                rel.ToWjGame();
            }
        }
    }
}
