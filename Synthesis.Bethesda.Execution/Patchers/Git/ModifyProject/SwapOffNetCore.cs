using System.Xml.Linq;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface ISwapOffNetCore
    {
        void Swap(XElement proj);
    }

    public class SwapOffNetCore : ISwapOffNetCore
    {
        public void Swap(XElement proj)
        {
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (elem.Name.LocalName.Equals("TargetFramework")
                        && elem.Value.Equals("netcoreapp3.1"))
                    {
                        elem.Value = "net6.0";
                    }
                }
            }
        }
    }
}