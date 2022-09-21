using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public class SubscriptionInputDto
{
    public bool enabled { get; set; }
    public string endpoint { get; set; }
    public DateTime? expirationTime { get; set; }
    public Keys keys { get; set; }
}

public class Keys
{
    public string p256dh { get; set; }
    public string auth { get; set; }
}