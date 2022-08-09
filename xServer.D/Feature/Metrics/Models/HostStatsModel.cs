using System;
using System.Collections.Generic;
using System.Text;

namespace x42.Feature.Metrics.Models
{
    public class HostStatsModel
    {
        public float ProcessorUtilizationPercent{ get; set; }
        public float AvailableMemoryMb { get; set; }
        public DateTime date { get; set; }
        public HostStatsModel(float processorUtilizationPercent, float availableMemoryMb)
        {
            ProcessorUtilizationPercent = processorUtilizationPercent;
            AvailableMemoryMb = availableMemoryMb;
            date = DateTime.Now;
        }
    }
}
