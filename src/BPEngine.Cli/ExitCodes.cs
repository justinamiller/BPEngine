using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPEngine.Cli
{
    /// <summary>
    /// 0 success; non-zero per error type.
    /// </summary>
        internal static class ExitCodes
        {
            public const int Ok = 0;
            public const int BadArgs = 1;
            public const int Error = 2;
            public const int NotFound = 3;
            public const int Invalid = 4;
        }
}
