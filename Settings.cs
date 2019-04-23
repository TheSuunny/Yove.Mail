using System.Collections.Generic;
using Yove.Http;

namespace Yove.Mail
{
    public abstract class Settings
    {
        internal HttpClient Client = new HttpClient
        {
            EnableProtocolError = false,
            UserAgent = HttpUtils.GenerateUserAgent()
        };

        internal string CSRFToken { get; set; }

        internal List<Message> SourceMessages = new List<Message>();
    }
}