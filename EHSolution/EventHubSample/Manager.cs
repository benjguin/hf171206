using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHubSample
{
    public class Manager
    {
        public static async Task CreateEventHub(string serviceBusConnectionString, 
            string eventHubName, int nbPartitions,
            string senderName, string senderKey, 
            string receiverName, string receiverKey,
            string consumerGroup1Name, string consumerGroup2Name,
            string consumerGroup3Name, string consumerGroup4Name)
        {

            NamespaceManager namespaceManager = NamespaceManager.CreateFromConnectionString(serviceBusConnectionString);

            EventHubDescription ehd = new EventHubDescription(eventHubName);
            ehd.PartitionCount = nbPartitions;
            ehd.MessageRetentionInDays = 1;
            ehd.Authorization.Add(new SharedAccessAuthorizationRule(senderName, senderKey, new AccessRights[] { AccessRights.Send }));
            ehd.Authorization.Add(new SharedAccessAuthorizationRule(receiverName, receiverKey, new AccessRights[] { AccessRights.Listen }));

            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Checking if event hub already exists");
            if (namespaceManager.EventHubExists(eventHubName))
            {
                Console.WriteLine("removing existing one");
                namespaceManager.DeleteEventHub(eventHubName);
            }

            Console.WriteLine("creating a new one");
            namespaceManager.CreateEventHub(ehd);

            Console.WriteLine("adding consumer groups");
            await Task.WhenAll(new Task[] {
                namespaceManager.CreateConsumerGroupAsync(ehd.Path, consumerGroup1Name),
                namespaceManager.CreateConsumerGroupAsync(ehd.Path, consumerGroup2Name),
                namespaceManager.CreateConsumerGroupAsync(ehd.Path, consumerGroup3Name),
                namespaceManager.CreateConsumerGroupAsync(ehd.Path, consumerGroup4Name)});
        }
    }
}
