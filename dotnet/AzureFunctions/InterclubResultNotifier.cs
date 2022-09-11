using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
    public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer, ILogger logger)
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

        await File.WriteAllTextAsync("results.html", content);

        HtmlDocument htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(content);

        foreach (HtmlNode updateNode in htmlDocument.DocumentNode.SelectNodes("//p[@class='update']"))
        {
            string value = updateNode.InnerText;
            logger.LogInformation(value);
        }
    }
}