using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace x42.Feature.Metrics
{
    public interface IMemoryMetricsService
    {
        MemoryMetrics GetMetrics();
    }
}
