using System;
using System.Collections.Generic;
using System.Text;

namespace x42.Feature.Metrics.Models
{
    public class HostStatsModel
    {
        public double ProcessorUtilizationPercent{ get; set; }
        public double AvailableMemoryMb { get; set; }
        public DateTime date { get; set; }
        public HostStatsModel(double processorUtilizationPercent, double freeMemory)
        {
            ProcessorUtilizationPercent = Math.Round(processorUtilizationPercent,0);
            AvailableMemoryMb = Math.Round(freeMemory,0);
            date = DateTime.UtcNow;
        }
    }
}
