using System;
using System.Collections.Generic;
using System.Text;

namespace Wrapper
{
    class Common
    {
        public static bool IsLinux
        {
            get
            {
                var p = (int)Environment.OSVersion.Platform;
                return (p == 4) || (p == 6) || (p == 128);
            }
        }
    }
}
