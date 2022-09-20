using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public class Notification
{
    public NotificationData notification { get; set; }
}

public class NotificationData
{
    public string title { get; set; }

    public string body { get; set; }

    public string icon { get; set; }

    public int[] vibrate { get; set; }

    public Dictionary<string, object> data { get; set; } = new Dictionary<string, object>();

    public List<NotificationAction> actions { get; set; } = new List<NotificationAction>();
}

public class NotificationAction
{
    public string action { get; set; }

    public string title { get; set; }
}