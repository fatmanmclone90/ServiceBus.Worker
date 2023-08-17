using System;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace ServiceBus.Worker
{
    public class ApplicationInsightsOperationWrapper : IApplicationInsightsOperationWrapper
    {
        private readonly TelemetryClient _telemetryClient;

        public ApplicationInsightsOperationWrapper(
            TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public async Task Track<T>(
            string operationName,
            Func<Task> func)
            where T : OperationTelemetry, new()
        {
            using var operation = _telemetryClient.StartOperation<T>(operationName);

            try
            {
                await func();
            }
            catch (Exception)
            {
                operation.Telemetry.Success = false;

                // See https://github.com/microsoft/ApplicationInsights-dotnet/issues/2248
                if (typeof(T) == typeof(RequestTelemetry))
                {
                    ((IOperationHolder<RequestTelemetry>)operation).Telemetry.ResponseCode = "500";
                }

                throw;
            }
        }
    }
}