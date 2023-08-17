using System.ComponentModel.DataAnnotations;

namespace ServiceBus.Worker
{
    public class ServiceBusOptions
    {
        [Required]
        public string QueueName { get; set; }

        // Summary:
        //     Gets or sets the maximum number of concurrent calls to the message handler the
        //     processor should initiate.
        //
        // Value:
        //     The maximum number of concurrent calls to the message handler. The default value
        //     is 1.
        [Required]
        public int MaxConcurrentCalls { get; set; }
        
        [Required]
        public string Namespace { get; set; }
    }
}