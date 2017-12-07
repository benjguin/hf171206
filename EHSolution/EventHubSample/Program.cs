using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubSample
{
    public class Program
    {
        public static object ConsoleSemaphore = new object();

        static void Main(string[] args)
        {
            try
            {
                AsyncMain().Wait();
            }
            catch (Exception ex)
            {
                lock (ConsoleSemaphore)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(ex);
                }
            }
            finally
            {
                lock(ConsoleSemaphore)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("done.");
                }
                Console.ReadLine();
            }
        }

        private static async Task AsyncMain()
        {
            string storageConnectionString=ConfigurationManager.AppSettings["storageConnectionString"];
            string serviceBusNamespace=ConfigurationManager.AppSettings["sb.namespace"];
            string sharedAccessKeyName=ConfigurationManager.AppSettings["sb.sharedAccessKeyName"];
            string sharedAccessKey=ConfigurationManager.AppSettings["sb.sharedAccessKey"];
            string eventHubName=ConfigurationManager.AppSettings["sb.eventHub.name"];
            int eventHubNbPartitions=int.Parse(ConfigurationManager.AppSettings["sb.eventHub.nbPartitions"]);
            string eventHub2Name = ConfigurationManager.AppSettings["sb.eventHub2.name"];
            int eventHub2NbPartitions = int.Parse(ConfigurationManager.AppSettings["sb.eventHub2.nbPartitions"]);
            string eventHubPolicySenderName =ConfigurationManager.AppSettings["sb.eventHub.policy.senderName"];
            string eventHubPolicySenderKey=ConfigurationManager.AppSettings["sb.eventHub.policy.senderKey"];
            string eventHubPolicyReceiverName=ConfigurationManager.AppSettings["sb.eventHub.policy.receiverName"];
            string eventHubPolicyReceiverKey=ConfigurationManager.AppSettings["sb.eventHub.policy.receiverKey"];
            int nbDevices=int.Parse(ConfigurationManager.AppSettings["sb.eventHub.sender.nbDevices"]);
            int nbEventsPerDevice=int.Parse(ConfigurationManager.AppSettings["sb.eventHub.sender.nbEventsPerDevice"]);
            string consumerGroup1Name = ConfigurationManager.AppSettings["sb.eventHub.consumerGroup1Name"];
            string consumerGroup2Name = ConfigurationManager.AppSettings["sb.eventHub.consumerGroup2Name"];
            string consumerGroup3Name = ConfigurationManager.AppSettings["sb.eventHub.consumerGroup3Name"];
            string consumerGroup4Name = ConfigurationManager.AppSettings["sb.eventHub.consumerGroup4Name"];

            bool dropCreateEventHubs = bool.Parse(ConfigurationManager.AppSettings["options.dropCreateEventHubs"]);
            bool sendEvents = bool.Parse(ConfigurationManager.AppSettings["options.sendEvents"]);
            bool receiveEvents = bool.Parse(ConfigurationManager.AppSettings["options.receiveEvents"]);
            int waitWhileSending = int.Parse(ConfigurationManager.AppSettings["options.waitMillisecondsWhileSending"]);

            string serviceBusConnectionString = GetConnectionStringFromConfiguration();

            string senderKey = eventHubPolicySenderKey == ""
                    ? SharedAccessAuthorizationRule.GenerateRandomKey()
                    : eventHubPolicySenderKey;

            string receiverKey = eventHubPolicyReceiverKey == ""
                    ? SharedAccessAuthorizationRule.GenerateRandomKey()
                    : eventHubPolicyReceiverKey;

            if (dropCreateEventHubs)
            {
                await Manager.CreateEventHub(
                    serviceBusConnectionString, eventHubName, eventHubNbPartitions,
                    eventHubPolicySenderName, senderKey,
                    eventHubPolicyReceiverName, receiverKey,
                    consumerGroup1Name, consumerGroup2Name,
                    consumerGroup3Name, consumerGroup4Name);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"event hub {eventHubName} created.");

                await Manager.CreateEventHub(
                    serviceBusConnectionString, eventHub2Name, eventHub2NbPartitions,
                    eventHubPolicySenderName, senderKey,
                    eventHubPolicyReceiverName, receiverKey,
                    consumerGroup1Name, consumerGroup2Name,
                    consumerGroup3Name, consumerGroup4Name);

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"event hub {eventHub2Name} created.");

                Console.WriteLine("ENTER to continue.");
                Console.ReadLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("configuration option: do not drop / create event hub.");
            }

            if (receiveEvents)
            {
                // receive in yellow
                Task receiveTask1 = Receiver.ReceiveEvents(serviceBusNamespace, eventHubName,
                    eventHubPolicyReceiverName, receiverKey,
                    consumerGroup1Name, 1,
                    ConsoleColor.Yellow,
                    storageConnectionString);

                // receive in green
                Task receiveTask2 = Receiver.ReceiveEvents(serviceBusNamespace, eventHubName,
                    eventHubPolicyReceiverName, receiverKey,
                    consumerGroup2Name, 2,
                    ConsoleColor.Green,
                    storageConnectionString);

                await receiveTask1;
                await receiveTask2;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("configuration option: do not receive events.");
            }

            if (sendEvents)
            {
                // send in Cyan
                Task sendTask = Sender.SendEvents(serviceBusNamespace, eventHubName,
                    eventHubPolicySenderName, senderKey,
                    nbDevices,
                    nbEventsPerDevice, waitWhileSending);

                await sendTask;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("configuration option: do not send events.");
            }
        }

        private static string GetConnectionStringFromConfiguration()
        {
            ServiceBusConnectionStringBuilder builder = new ServiceBusConnectionStringBuilder()
            {
                SharedAccessKeyName = ConfigurationManager.AppSettings["sb.sharedAccessKeyName"],
                SharedAccessKey = ConfigurationManager.AppSettings["sb.sharedAccessKey"]
            };
            builder.Endpoints.Add(
                ServiceBusEnvironment.CreateServiceUri("sb", 
                    ConfigurationManager.AppSettings["sb.namespace"], ""));

            builder.TransportType = TransportType.Amqp;
            return builder.ToString();
        }
    }
}
