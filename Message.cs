using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Yove.Mail
{
    public class Message : Settings
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }

        public DateTime Date { get; set; }

        public List<(string Filename, long Size, string MimeType, string URL)> Attachments = new List<(string Filename, long Size, string MimeType, string URL)>();

        public async Task<bool> Delete()
        {
            string Delete = await Client.GetString($"https://api4.temp-mail.org/request/delete/id/{Id}/format/json");

            if ((string)JObject.Parse(Delete)["result"] == "success")
            {
                SourceMessages.Remove(this);
                return true;
            }

            return false;
        }
    }
}