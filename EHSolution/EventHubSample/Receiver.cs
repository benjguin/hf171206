using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubSample
{
    public class Receiver
    {
        public static async Task ReceiveEvents(string serviceBusNamespace, string eventHubName,
            string receiverName, string receiverKey,
            string consumerGroupName, int receptionNumber, ConsoleColor consoleColor, 
            string storageConnectionString /* Required for checkpoint/state */)
        {
            lock(Program.ConsoleSemaphore)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(">>> for received messages");
           }

            string eventHubReceiveConnectionString = 
                string.Format("Endpoint=sb://{0}.servicebus.windows.net/;SharedAccessKeyName={1};SharedAccessKey={2}",
                serviceBusNamespace, receiverName, receiverKey);

            EventProcessorHost eventProcessorHost = new EventProcessorHost("host-" + consumerGroupName,
                eventHubName, consumerGroupName, 
                eventHubReceiveConnectionString,
                storageConnectionString);

            if (receptionNumber == 1)
            {
                SimpleEventProcessor1.ClassConsoleColor = consoleColor;
                await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor1>();
            }
            else
            {
                SimpleEventProcessor2.ClassConsoleColor = consoleColor;
                await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor2>();
            }
        }
    }

    public abstract class SimpleEventProcessor : IEventProcessor
    {
        PartitionContext partitionContext;
        Stopwatch checkpointStopWatch;

        protected abstract void ChangeConsoleColor();

        public Task OpenAsync(PartitionContext context)
        {
            lock(Program.ConsoleSemaphore)
            {
                ChangeConsoleColor();
                Console.Write(this.GetType().Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(
                    string.Format(
                        " initialize.  Partition: '{0}', Offset: '{1}'",
                        context.Lease.PartitionId, context.Lease.Offset));
            }
            this.partitionContext = context;
            this.checkpointStopWatch = new Stopwatch();
            this.checkpointStopWatch.Start();
            return Task.FromResult<object>(null);
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> events)
        {
            try
            {
                foreach (EventData eventData in events)
                {
                    dynamic newData = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(eventData.GetBytes()));

                    lock(Program.ConsoleSemaphore)
                    {
                        ChangeConsoleColor();
                        Console.WriteLine(string.Format(">>> Device: '{0}', Measure1: '{1}', (from partition {2})",
                            newData.deviceId, newData.measure1, partitionContext.Lease.PartitionId));
                    }
                }

                //Call checkpoint every 5 minutes, so that worker can resume processing from the 5 minutes back if it restarts.
                if (this.checkpointStopWatch.Elapsed > TimeSpan.FromMinutes(5))
                {
                    await context.CheckpointAsync();
                    this.checkpointStopWatch.Restart();
                }
            }
            catch (Exception exp)
            {
                lock(Program.ConsoleSemaphore)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine("Error in processing: " + exp.Message);
                }
            }
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine(string.Format("Processor Shuting Down.  Partition '{0}', Reason: '{1}'.", this.partitionContext.Lease.PartitionId, reason.ToString()));
            if (reason == CloseReason.Shutdown)
            {
                await context.CheckpointAsync();
            }
        }
    }

    public class SimpleEventProcessor1 : SimpleEventProcessor
    {
        public static ConsoleColor ClassConsoleColor;
        protected override void ChangeConsoleColor()
        {
            Console.ForegroundColor = ClassConsoleColor;
        }
    }

    public class SimpleEventProcessor2 : SimpleEventProcessor
    {
        public static ConsoleColor ClassConsoleColor;
        protected override void ChangeConsoleColor()
        {
            Console.ForegroundColor = ClassConsoleColor;
        }
    }
}
