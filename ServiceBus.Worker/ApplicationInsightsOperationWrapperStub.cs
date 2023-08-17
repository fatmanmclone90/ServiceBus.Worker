using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace ServiceBus.Worker
{
    public class ApplicationInsightsOperationWrapperStub : IApplicationInsightsOperationWrapper
    {
        public async Task Track<T>(string operationName, Func<Task> func) where T : OperationTelemetry, new()
        {
            await func();
        }
    }
}