using System.Collections.Generic;
using Yove.Http;

namespace Yove.Mail
{
    public abstract class Settings
    {
        internal HttpClient Client = new HttpClient
        {
            EnableProtocolError = false,
            UserAgent = HttpUtils.GenerateUserAgent(),
            EnableCookies = true,
            Cookies = new System.Collections.Specialized.NameValueCollection()
        };

        internal List<Message> SourceMessages = new List<Message>();

        internal string Hash { get; set; }
    }
}