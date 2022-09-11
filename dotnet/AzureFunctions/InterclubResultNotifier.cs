using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SnookerLimburg.AzureFunctions;

public class InterclubResultNotifier
{
    // Timer
    // */5 * * * * * // 5 seconds
    // 0 */1 * * * * // 1 minute
    [FunctionName("InterclubResultNotifier")]
    public async Task Run(
        [TimerTrigger("0 0 */1 * * *")] TimerInfo timer,
        [Table("InterclubResultNotifierUpdates")] TableClient updatesTableClient,
        ILogger logger)
    {
        logger.LogInformation($"Interclub result notifier executed at: {DateTime.Now}");

        Uri baseAddress = new Uri("https://www.snookerlimburg.be");

        CookieContainer cookieContainer = new CookieContainer();
        cookieContainer.Add(baseAddress, new Cookie("jwts_tab1jwTabsCookie", "2"));

        using HttpClientHandler httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
        using HttpClient httpClient = new HttpClient(httpClientHandler) { BaseAddress = baseAddress };

        HttpResponseMessage response = await httpClient.GetAsync("/index.php?option=com_content&view=article&id=1&test=0&Itemid=112");
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();

        // TODO: Remove?
        //await File.WriteAllTextAsync("results.html", content);

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        foreach (var (node, index) in htmlDocument.DocumentNode.SelectNodes("//p[@class='update']").Select((node, index) => (node, index)))
        {
            string dateAsString = node.InnerText.Replace("update ", string.Empty);

            DateTimeOffset date = DateTimeOffset.ParseExact(dateAsString, "yyyy-MM-dd HH:mm", null);

            string lastUpdatedKey = "LastUpdated";
            TableEntity updateEntity = await updatesTableClient.GetEntityAsync<TableEntity>(string.Empty, index.ToString());

            if (updateEntity != null)
            {
                if ((DateTimeOffset)updateEntity[lastUpdatedKey] != date)
                {
                    updateEntity[lastUpdatedKey] = date;
                    await updatesTableClient.UpdateEntityAsync(updateEntity, ETag.All);

                    // TODO: Send notification
                }
            }
            else
            {
                updateEntity = new TableEntity(string.Empty, index.ToString());
                updateEntity.Add(lastUpdatedKey, date);

                await updatesTableClient.AddEntityAsync(updateEntity);
            }
        }
    }
}