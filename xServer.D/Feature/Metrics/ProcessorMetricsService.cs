using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace x42.Feature.Metrics
{
    public class ProcessorMetrics
    {
        public double ProcessorTime;
    }

    public class ProcessorMetricsService : IProcessorMetricsService
    {
        private readonly ILogger _logger;
        public IRuntimeInformationService RuntimeInformationService { get; set; }
        public ProcessorMetricsService(IRuntimeInformationService runtimeInformationService,
            ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType().FullName);
            this.RuntimeInformationService = runtimeInformationService;
        }
        public ProcessorMetrics GetMetrics()
        {
            if (RuntimeInformationService.IsUnix())
            {
                return GetUnixMetrics();
            }

            return GetWindowsMetrics();
        }

        private ProcessorMetrics GetWindowsMetrics()
        {
            var output = "";

            var info = new ProcessStartInfo();
            info.FileName = "wmic";
            info.Arguments = "CPU get Name,LoadPercentage /Value";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Trim().Split("\n");

            var CpuUse = lines[0].Split("=", StringSplitOptions.RemoveEmptyEntries);
            //var CpuName = lines[1].Split("=", StringSplitOptions.RemoveEmptyEntries)[1];

            ProcessorMetrics metrics = new ProcessorMetrics();
            metrics.ProcessorTime = Math.Round(double.Parse(CpuUse[1]), 0);
            return metrics;

        }

        private ProcessorMetrics GetUnixMetrics()
        {

            var output = "";
            var info = new ProcessStartInfo("/bin/bash");
            //info.FileName = "";
            info.Arguments = "-c \"top -bn1 | grep \'Cpu(s)\' | sed \'s/.*, *\\([0-9.]*\\)%* id.*/\\1/\' | awk '{print 100 - $1}' \" ";
            info.RedirectStandardOutput = true;

            using (var process = Process.Start(info))
            {
                output = process.StandardOutput.ReadToEnd();
            }

            var lines = output.Split("\n");
            var processorUsage = lines[0].Split("", StringSplitOptions.RemoveEmptyEntries);

            var metrics = new ProcessorMetrics();
            metrics.ProcessorTime = double.Parse(processorUsage[0]);
            return metrics;
        }
    }

}
