using System.Collections.Generic;
using Yove.Http;
using Yove.Mail.Models;

namespace Yove.Mail
{
    internal static class Settings
    {
        internal static HttpClient Client = new HttpClient
        {
            EnableProtocolError = false,
            UserAgent = HttpUtils.GenerateUserAgent(),
            EnableCookies = true
        };

        internal static List<Message> AllMessages = new List<Message>();

        internal static string Hash { get; set; }
    }
}