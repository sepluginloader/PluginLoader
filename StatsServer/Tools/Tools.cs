using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using avaness.StatsServer.Persistence;

namespace avaness.StatsServer.Tools
{
    public static class Tools
    {
        public static string FormatDateIso8601(DateTime dt) => dt.ToString("s")[..10];

        public static string SanitizeFileName(string name)
        {
            var sb = new StringBuilder();
            foreach (var c in name.Trim())
            {
                switch (c)
                {
                    case >= '0' and <= '9':
                    case >= 'a' and <= 'z':
                    case >= 'A' and <= 'Z':
                    case '-':
                    case '+':
                        sb.Append(c);
                        break;

                    default:
                        sb.Append('_');
                        break;
                }
            }

            return sb.ToString();
        }

        private static JsonSerializerOptions jsonOptionsCache;

        public static JsonSerializerOptions JsonOptions => jsonOptionsCache ??= new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private static readonly Regex PlayerHashRegex = new(@"^[a-z0-9]{20}$");
        public static bool ValidatePlayerHash(string playerHash) => playerHash != null && PlayerHashRegex.IsMatch(playerHash);

        private static readonly UTF8Encoding Utf8 = new();

        public static string Sha1HexDigest(string text)
        {
            var buffer = Utf8.GetBytes(text);
            var digest = SHA1.HashData(buffer);
            return BytesToHex(digest);
        }

        private static string BytesToHex(IReadOnlyCollection<byte> ba)
        {
            var hex = new StringBuilder(2 * ba.Count);

            foreach (var t in ba)
                hex.Append(t.ToString("x2"));

            return hex.ToString();
        }
    }
}