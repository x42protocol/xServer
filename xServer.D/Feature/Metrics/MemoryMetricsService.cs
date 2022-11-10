using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.Metrics
{
    public class MemoryMetrics
    {
        public double Total;
        public double Used;
        public double Free;
    }

    public class MemoryMetricsService : IMemoryMetricsService
    {
        public IRuntimeInformationService RuntimeInformationService { get; set; }
        public MemoryMetricsService(IRuntimeInformationService runtimeInformationService)
        {
            this.RuntimeInformationService = runtimeInformationService;
        }
        public MemoryMetrics GetMetrics()
        {
            if (RuntimeInformationService.IsUnix())
            {
                return GetUnixMetrics();
            }

            return GetWindowsMetrics();
        }

        private MemoryMetrics GetWindowsMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");
            var freeMemoryParts = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            var totalMemoryParts = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new MemoryMetrics();
            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1]) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1]) / 1024, 0);
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }

        private MemoryMetrics GetUnixMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo("/bin/bash");
            info.Arguments = "-c \"free -m\"";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Split("\n");
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            var metrics = new MemoryMetrics();
            metrics.Total = double.Parse(memory[1]);
            metrics.Used = double.Parse(memory[2]);
            metrics.Free = double.Parse(memory[3]);
            return metrics;
        }
    }

}
