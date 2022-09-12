string text = "VRIJ|0 - 9|Happy Snooker C|Re-Spot A|&nbsp; - &nbsp;|Zuma B|Buckingham A|9 - 0|VRIJ|Re-Spot B|&nbsp; - &nbsp;|Re-Spot C|Zuma A|&nbsp; - &nbsp;|Biljart Lounge A|Riley Inn C|&nbsp; - &nbsp;|Riley Inn B|Riley Inn A|&nbsp; - &nbsp;|Happy Snooker A|Happy Snooker B|&nbsp; - &nbsp;|De Kreeft A|Buckingham C|&nbsp; - &nbsp;|Buckingham B|";

string[] textArr = text.Split('|');

foreach (string textItem in textArr)
{
    Console.WriteLine(textItem);
}

Console.ReadKey();