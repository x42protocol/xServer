using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.Metrics
{
    public interface IRuntimeInformationService
    {
        string GetFrameworkDescription();
        Architecture GetOSArchitecture();
        string GetOSDescription();
        OSPlatform GetOsPlatform();
        Architecture GetProcessArchitecture();
        string GetRuntimeIdentifier();
        bool IsFreeBSD();
        bool IsLinux();
        bool IsOSX();
        bool IsUnix();
        bool IsWindows();
    }
}
