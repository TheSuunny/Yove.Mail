using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using static Yove.Mail.Settings;

namespace Yove.Mail.Models
{
    public class Message
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }

        public DateTime Date { get; set; }

        public List<Attachment> Attachments = new List<Attachment>();

        public async Task<bool> Delete()
        {
            JToken Json = await Client.GetJson($"https://api4.temp-mail.org/request/delete/id/{Id}/format/json");

            if ((string)Json["result"] == "success")
            {
                AllMessages.Remove(this);
                return true;
            }

            return false;
        }
    }
}