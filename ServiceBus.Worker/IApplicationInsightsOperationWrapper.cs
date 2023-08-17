using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace ServiceBus.Worker
{
    public interface IApplicationInsightsOperationWrapper
    {
        Task Track<T>(
            string operationName,
            Func<Task> func)
         where T : OperationTelemetry, new();
    }
}
