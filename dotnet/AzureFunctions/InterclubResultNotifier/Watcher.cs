using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using HtmlAgilityPack;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public class Watcher
{
    private ILogger _logger;
    private TableClient _resultsTableClient;
    private TimerInfo _timer;
    private TableClient _updatesTableClient;
    private QueueClient _notificationsQueueClient;

    [FunctionName("InterclubResultNotifierWatcher")]
    public async Task Run(
        ILogger logger,
        [Table("InterclubResultNotifierResults")] TableClient resultsTableClient,
        [TimerTrigger("0 */5 * * * *")] TimerInfo timer,
        [Table("InterclubResultNotifierUpdates")] TableClient updatesTableClient,
        [Queue("interclubresultnotifiernotifications")] QueueClient notificationsQueueClient)
    {
        _logger = logger;
        _resultsTableClient = resultsTableClient;
        _timer = timer;
        _updatesTableClient = updatesTableClient;
        _notificationsQueueClient = notificationsQueueClient;

        _logger.LogInformation($"Interclub result notifier watcher executed at: {DateTime.Now}");

        await CheckResultsAsync();
    }

    private async Task CheckResultsAsync()
    {
        Uri baseAddress = new Uri("https://www.snookerlimburg.be");

        CookieContainer cookieContainer = new CookieContainer();
        cookieContainer.Add(baseAddress, new Cookie("jwts_tab1jwTabsCookie", "2"));

        using HttpClientHandler httpClientHandler = new HttpClientHandler() { CookieContainer = cookieContainer };
        using HttpClient httpClient = new HttpClient(httpClientHandler) { BaseAddress = baseAddress };

        HttpResponseMessage response = await httpClient.GetAsync("/index.php?option=com_content&view=article&id=1&test=0&Itemid=112");
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        foreach (var (node, index) in htmlDocument.DocumentNode.SelectNodes("//p[@class='update']").Select((node, index) => (node, index)))
        {
            string dateAsString = node.InnerText.Replace("update ", string.Empty);
            DateTimeOffset date = DateTimeOffset.ParseExact($"{dateAsString} +2", "yyyy-MM-dd HH:mm z", null);

            string lastUpdatedKey = "LastUpdated";
            TableEntity updateEntity = await _updatesTableClient.GetEntityAsync<TableEntity>(string.Empty, index.ToString());

            if (updateEntity != null)
            {
                if ((DateTimeOffset)updateEntity[lastUpdatedKey] != date)
                {
                    updateEntity[lastUpdatedKey] = date;
                    await _updatesTableClient.UpdateEntityAsync(updateEntity, ETag.All);

                    await CalculateNewResultAsync(htmlDocument, division: index);
                }
            }
            else
            {
                updateEntity = new TableEntity(string.Empty, index.ToString());
                updateEntity.Add(lastUpdatedKey, date);

                await _updatesTableClient.AddEntityAsync(updateEntity);

                await CalculateNewResultAsync(htmlDocument, division: index);
            }
        }
    }

    private async Task CalculateNewResultAsync(HtmlDocument htmlDocument, int division)
    {
        HtmlNodeCollection htmlNodes = htmlDocument.DocumentNode.SelectNodes($"//table[@class='ic-result ic-rks{division + 1}']//tr//td");

        List<Result> results = new List<Result>();
        Result result = new Result();
        int columnIndex = 0;

        foreach (var htmlNode in htmlNodes)
        {
            if (htmlNode.InnerText == string.Empty)
            {
                results.Add(result);
                columnIndex = 0;

                continue;
            }

            switch (columnIndex)
            {
                case 0:
                    result = new Result();
                    result.Date = DateTimeOffset.ParseExact(
                        $"{htmlNode.InnerText.Split("&nbsp;")[1]} +2",
                        "dd-MM-yy z",
                        null);
                    break;

                case 1:
                    result.Home = htmlNode.InnerText;
                    break;

                case 2:
                    if (htmlNode.InnerHtml.Contains("<strong>"))
                    {
                        string[] scoreArr = htmlNode.InnerText.Split("-");

                        result.HomeScore = int.Parse(scoreArr[0].Trim());
                        result.AwayScore = int.Parse(scoreArr[1].Trim());
                    }

                    break;

                case 3:
                    result.Away = htmlNode.InnerText;
                    break;
            }

            columnIndex++;
        }

        foreach (Result result2 in results)
        {
            if (!result2.HomeScore.HasValue || !result2.AwayScore.HasValue) { continue; }

            TableEntity resultEntity = new TableEntity(string.Empty, Guid.NewGuid().ToString());
            resultEntity.Add("MD5", result2.MD5);
            resultEntity.Add("Date", result2.Date);
            resultEntity.Add("Division", MapDivisionToText(division));
            resultEntity.Add("Home", result2.Home);
            resultEntity.Add("HomeScore", result2.HomeScore);
            resultEntity.Add("AwayScore", result2.AwayScore);
            resultEntity.Add("Away", result2.Away);

            var existingEntityQuery = _resultsTableClient.QueryAsync<TableEntity>(filter: $"PartitionKey eq '' and MD5 eq '{result2.MD5}'", maxPerPage: 1);
            TableEntity existingEntity = null;

            await foreach (var page in existingEntityQuery.AsPages())
            {
                if (existingEntity != null)
                {
                    continue;
                }

                existingEntity = page.Values.FirstOrDefault();
            }

            if (existingEntity != null)
            {
                continue;
            }

            await _resultsTableClient.AddEntityAsync(resultEntity);

            // Send notification
            await _notificationsQueueClient.SendMessageAsync(
                $"{MapDivisionToText(division)} afdeling" + Environment.NewLine +
                $"{result2.Home} {result2.HomeScore} - {result2.AwayScore} {result2.Away}");
        }
    }

    private string MapDivisionToText(int division)
    {
        switch (division)
        {
            case 0: return "Ere";
            case 1: return "1ste";
            case 2:
            case 3:
            case 4:
            case 5:
                return $"{division}de";

            case 6: return "Zaterdag";
            default:
                return $"{division}de";
        }
    }
}