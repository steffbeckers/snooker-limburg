using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SnookerLimburg.AzureFunctions;

public class InterclubResultNotifier
{
    [FunctionName("InterclubResultNotifier")]
    public void Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"Interclub result notifier executed at: {DateTime.Now}");
    }
}