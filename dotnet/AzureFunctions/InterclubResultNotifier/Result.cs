using SnookerLimburg.Extensions;
using System;

namespace SnookerLimburg.AzureFunctions.InterclubResultNotifier;

public class Result
{
    public string Away { get; set; }
    public int? AwayScore { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string Home { get; set; }
    public int? HomeScore { get; set; }
    public string MD5 { get => (Date + Home + HomeScore + AwayScore + Away).CreateMD5(); }
}