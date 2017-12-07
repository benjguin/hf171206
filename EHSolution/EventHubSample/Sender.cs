using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubSample
{
    public class Sender
    {
        public static async Task SendEvents(string serviceBusNamespace, string eventHubName, 
            string senderName, string senderKey, 
            int nbDevices, int nbEventsPerDevice, int waitWhileSending)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var settings = new MessagingFactorySettings()
            {
                TokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(senderName, senderKey),
                TransportType = TransportType.Amqp
            };
            var factory = MessagingFactory.Create(
                 ServiceBusEnvironment.CreateServiceUri("sb", serviceBusNamespace, ""), settings);
            EventHubClient client = factory.CreateEventHubClient(eventHubName);
            
            Random random = new Random();

            lock (Program.ConsoleSemaphore)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("sent events finish with >>>");
            }

            for (int m=0; m<nbEventsPerDevice; m++)
            {
                var tasks = new HashSet<Task>();

                if (waitWhileSending > 0)
                {
                    System.Threading.Thread.Sleep(waitWhileSending);
                }

                for (int d=0; d<nbDevices; d++)
                {
                    var now = DateTime.UtcNow;
                    dynamic e = new JObject();
                    e.deviceId = d;
                    e.measure1 = random.Next(0,100);
                    e.sendTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
                    e.sendTimestamp = Convert.ToInt64(Math.Round(now.Subtract(epoch).TotalMilliseconds));
                    string eAsJsonString = JsonConvert.SerializeObject(e);
                    System.Diagnostics.Debug.WriteLine(eAsJsonString);
                    EventData data = new EventData(Encoding.UTF8.GetBytes(eAsJsonString))
                    {
                        PartitionKey = d.ToString()
                    };

                    lock (Program.ConsoleSemaphore)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("Device: '{0}', Measure1: '{1}' >>>", e.deviceId, e.measure1);
                    }
                    tasks.Add(client.SendAsync(data));
                }

                await Task.WhenAll(tasks);
            }
        }
    }
}
