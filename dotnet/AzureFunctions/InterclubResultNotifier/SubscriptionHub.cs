using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WebPush;
using Microsoft.Extensions.Configuration;
using Azure.Data.Tables;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public static class SubscriptionHub
{
    [FunctionName("InterclubResultNotifierSubscriptionHub")]
    public static async Task<IActionResult> Run(
        ILogger logger,
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        [Table("InterclubResultNotifierSubscriptions")] TableClient subscriptionsTableClient)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("settings.json", optional: true, reloadOnChange: true)
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        SubscriptionInputDto input = JsonConvert.DeserializeObject<SubscriptionInputDto>(requestBody);

        PushSubscription pushSubscription = new PushSubscription(
            input.endpoint,
            input.keys.p256dh,
            input.keys.auth);

        if (input.enabled)
        {
            TableEntity pushSubscriptionEntity = new TableEntity(string.Empty, pushSubscription.Auth);
            pushSubscriptionEntity.Add("Endpoint", pushSubscription.Endpoint);
            pushSubscriptionEntity.Add("P256DH", pushSubscription.P256DH);
            pushSubscriptionEntity.Add("Auth", pushSubscription.Auth);

            await subscriptionsTableClient.AddEntityAsync(pushSubscriptionEntity);

            await SendTestNotificationAsync(configuration, pushSubscription);
        }
        else
        {
            await subscriptionsTableClient.DeleteEntityAsync(string.Empty, pushSubscription.Auth);
        }

        return new OkResult();
    }

    private static async Task SendTestNotificationAsync(
        IConfiguration configuration,
        PushSubscription pushSubscription)
    {
        WebPushClient webPushClient = new WebPushClient();

        webPushClient.SetVapidDetails(new VapidDetails(
            subject: "mailto:steff@steffbeckers.eu",
            publicKey: configuration.GetValue<string>("InterclubResultNotifier:Vapid:PublicKey"),
            privateKey: configuration.GetValue<string>("InterclubResultNotifier:Vapid:PrivateKey")));

        await webPushClient.SendNotificationAsync(
            pushSubscription,
            JsonConvert.SerializeObject(new Notification()
            {
                notification = new NotificationData()
                {
                    title = "Test notification",
                    body = "Successfully subscribed!"
                }
            }));
    }
}