﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace serialPortStd.Methods
{
    class PlatformHelper
    {
        public static bool IsWindows() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool IsMacOS() =>  RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool IsLinux() =>  RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }
}
