using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class SettingsNodeVM : ViewModel
    {
        public string MemberName { get; }
        public Type TargetType { get; }

        public SettingsNodeVM(string memberName, Type targetType)
        {
            MemberName = memberName;
            TargetType = targetType;
        }
    }
}
