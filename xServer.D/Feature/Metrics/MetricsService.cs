using x42.Feature.Metrics.Models;
using System.Diagnostics;

namespace x42.Feature.Metrics
{
    public class MetricsService
    {
        private PerformanceCounter _cpuCounter { get; set; }
        private PerformanceCounter _ramCounter { get; set; }
        public MetricsService()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); //Memory array - last 10 minutes, ping every 10 seconds - round robin.
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        public HostStatsModel GetHardwareMetric()
        {
            return new HostStatsModel(this._cpuCounter.NextValue(), this._ramCounter.NextValue());
        }
    }
}
