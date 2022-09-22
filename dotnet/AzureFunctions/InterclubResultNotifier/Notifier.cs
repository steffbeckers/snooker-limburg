using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;
using WebPush;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public class Notifier
{
    [FunctionName("InterclubResultNotifier")]
    public async Task Run(
        [QueueTrigger("interclubresultnotifiernotifications")] string message,
        [Table("InterclubResultNotifierSubscriptions")] TableClient subscriptionsTableClient,
        ILogger logger)
    {
        logger.LogInformation($"C# Queue trigger function processed: {message}");

        Notification notification = JsonConvert.DeserializeObject<Notification>(message);

        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        WebPushClient webPushClient = new WebPushClient();

        webPushClient.SetVapidDetails(new VapidDetails(
            subject: "mailto:steff@steffbeckers.eu",
            publicKey: configuration.GetValue<string>("InterclubResultNotifier:Vapid:PublicKey"),
            privateKey: configuration.GetValue<string>("InterclubResultNotifier:Vapid:PrivateKey")));

        await foreach (Page<TableEntity> page in subscriptionsTableClient.QueryAsync<TableEntity>().AsPages())
        {
            foreach (TableEntity sub in page.Values)
            {
                PushSubscription pushSubscription = new PushSubscription(
                    endpoint: sub.GetString("Endpoint"),
                    p256dh: sub.GetString("P256DH"),
                    auth: sub.GetString("Auth"));

                await webPushClient.SendNotificationAsync(
                    pushSubscription,
                    JsonConvert.SerializeObject(notification));
            }
        }
    }
}