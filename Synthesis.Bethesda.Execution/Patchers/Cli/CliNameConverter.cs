using System;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Cli
{
    public interface ICliNameConverter
    {
        string Convert(FilePath path);
    }

    public class CliNameConverter : ICliNameConverter
    {
        public string Convert(FilePath path)
        {
            try
            {
                return path.NameWithoutExtension;
            }
            catch (Exception)
            {
                return "<Naming Error>";
            }
        }
    }
}