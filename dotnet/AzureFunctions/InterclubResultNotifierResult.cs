using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnookerLimburg.AzureFunctions;

public class InterclubResultNotifierResult
{
    public string Away { get; set; }
    public int? AwayScore { get; set; }
    public DateTimeOffset? Date { get; set; }
    public string Home { get; set; }
    public int? HomeScore { get; set; }
    public string MD5 { get => (Date + Home + HomeScore + AwayScore + Away).CreateMD5(); }
}