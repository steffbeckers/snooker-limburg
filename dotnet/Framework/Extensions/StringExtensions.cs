using System;
using System.Security.Cryptography;
using System.Text;

namespace SnookerLimburg.Extensions;

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