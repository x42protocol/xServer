using x42.Feature.Metrics.Models;
using System.Diagnostics;
using System.Threading.Tasks;

namespace x42.Feature.Metrics
{
    public class MetricsService
    {
        private readonly IMemoryMetricsService _memoryMetricsService;
        private readonly IProcessorMetricsService _processorMetricsService;
        private double _cpuTotalTime { get; set; }
        private double _freeMemory { get; set; }
        public MetricsService(IMemoryMetricsService memoryMetricsService, IProcessorMetricsService processorMetricsService)
        {
            _memoryMetricsService = memoryMetricsService;
            _processorMetricsService = processorMetricsService;
        }

        public HostStatsModel GetHardwareMetric()
        {
            _cpuTotalTime = _processorMetricsService.GetMetrics().ProcessorTime;

            _freeMemory = _memoryMetricsService.GetMetrics().Free;

            return new HostStatsModel(_cpuTotalTime, _freeMemory);
        }
    }
}
