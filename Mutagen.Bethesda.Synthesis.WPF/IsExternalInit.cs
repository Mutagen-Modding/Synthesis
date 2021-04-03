using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if NET_5
#else
namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit
    {
    }
}
#endif
