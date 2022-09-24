using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.Metrics
{
    public class RuntimeInformationService : IRuntimeInformationService
    {
        public OSPlatform GetOsPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OSPlatform.OSX;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OSPlatform.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return OSPlatform.FreeBSD;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OSPlatform.Windows;

            return OSPlatform.Create("Unknown");
        }

        public bool IsUnix()
        {
            var isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            return isUnix;
        }
        public bool IsWindows()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }
        public bool IsLinux()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        }
        public bool IsFreeBSD()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
        }
        public bool IsOSX()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }
        public string GetRuntimeIdentifier()
        {
            return RuntimeInformation.RuntimeIdentifier;
        }
        public Architecture GetProcessArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture;
        }

        public Architecture GetOSArchitecture()
        {
            return RuntimeInformation.OSArchitecture;
        }

        public string GetOSDescription()
        {
            return RuntimeInformation.OSDescription;
        }
        public string GetFrameworkDescription()
        {
            return RuntimeInformation.FrameworkDescription;
        }


    }

}
