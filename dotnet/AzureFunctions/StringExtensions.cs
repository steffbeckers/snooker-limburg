using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SnookerLimburg.AzureFunctions;

public static class StringExtensions
{
    public static string CreateMD5(this string input)
    {
        using MD5 md5 = MD5.Create();

        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes);
    }
}