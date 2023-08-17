using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ServiceBus.Worker
{
    internal class QueueListener : BackgroundService
    {
        private readonly ServiceBusClient _client;
        private readonly ServiceBusProcessor _processor;
        private readonly IApplicationInsightsOperationWrapper _applicationInsightsOperationWrapper;

        public QueueListener(
            IApplicationInsightsOperationWrapper applicationInsightsOperationWrapper,
            IOptions<ServiceBusOptions> serviceBusOptions)
        {
            _applicationInsightsOperationWrapper = applicationInsightsOperationWrapper;
            var clientOptions = new ServiceBusClientOptions
            {
                TransportType = ServiceBusTransportType.AmqpTcp
            };
            _client = new ServiceBusClient(
                serviceBusOptions.Value.Namespace,
                new DefaultAzureCredential(),
                clientOptions);
            _processor = _client.CreateProcessor(
               serviceBusOptions.Value.QueueName,
               new ServiceBusProcessorOptions
               {
                   MaxConcurrentCalls = serviceBusOptions.Value.MaxConcurrentCalls,
                   PrefetchCount = 0,
               });
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            await _processor.StartProcessingAsync(stoppingToken);
            await tcs.Task;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
            await _client.DisposeAsync();
        }

        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            await _applicationInsightsOperationWrapper.Track<RequestTelemetry>(
                "some-request",
                async () =>
                {
                    var body = args.Message.Body.ToString();

                    // DO WORK

                    await args.CompleteMessageAsync(args.Message);
                });
        }

        // handle any errors when receiving messages
        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            // DO ERROR HANDLING

            return Task.CompletedTask;
        }
    }
}