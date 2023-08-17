using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights.Extensibility.EventCounterCollector;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceBus.Worker
{
    public static class Program
    {
        /// <summary>
        /// https://learn.microsoft.com/en-us/dotnet/core/diagnostics/available-counters
        /// </summary>
        private static List<(string namespsace, string metric)> RuntimeMetrics => new()
        {
            ( SystemRuntimeNamespace, "threadpool-completed-items-count" ),
            ( SystemRuntimeNamespace, "threadpool-queue-length" )   ,
            ( SystemRuntimeNamespace, "threadpool-thread-count" ) ,
            ( SystemNetHttpNamespace, "http11-requests-queue-duration" ),
            ( SystemNetDnsNamespace, "dns-lookups-duration"),
        };
        private const string SystemRuntimeNamespace = "System.Runtime";
        private const string SystemNetHttpNamespace = "System.Net.Http";
        private const string SystemNetDnsNamespace = "System.Net.NameResolution";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder();

            builder.Services.AddControllers();
            builder.Services.AddHealthChecks();
            builder.Configuration.AddEnvironmentVariables();
            builder.Host.UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                options.ValidateOnBuild = true;
            });
            builder.Logging.ClearProviders();
            builder.Logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Warning);

            builder.Services
                .AddOptions<ServiceBusOptions>()
                .BindConfiguration(nameof(ServiceBusOptions))
                .ValidateDataAnnotations();

            builder.Services.AddHostedService<QueueListener>();

            builder.Services.ConfigureApplicationInsights();

            var app = builder.Build();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health");
            });

            app.Run();
        }

        public static void ConfigureApplicationInsights(this IServiceCollection services)
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY")))
            {
                services.AddApplicationInsightsTelemetry(options =>
                {
                    options.EnableAdaptiveSampling = false;
                });
                services.ConfigureTelemetryModule<EventCounterCollectionModule>(
                (module, o) =>
                {
                    o.EnableAdaptiveSampling = false;
                    module.Counters.Clear();
                    foreach (var (@namespace, metric) in RuntimeMetrics)
                    {
                        module.Counters.Add(
                            new EventCounterCollectionRequest(
                            @namespace,
                            metric));
                    }
                });

                services.AddSingleton<IApplicationInsightsOperationWrapper, ApplicationInsightsOperationWrapper>();
            }
            else
            {
                services.AddSingleton<IApplicationInsightsOperationWrapper, ApplicationInsightsOperationWrapperStub>();
            }
        }
    }
}