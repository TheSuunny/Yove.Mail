using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MimeKit;

namespace Yove.Mail
{
    public class Message : Settings
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Subject { get; set; }
        public string TextBody { get; set; }
        public string HtmlBody { get; set; }

        public DateTimeOffset Date { get; set; }

        public IEnumerable<MimeEntity> Attachments { get; set; }

        public async Task Delete()
        {
            await Client.Get($"https://temp-mail.org/en/delete/{Id}/").ConfigureAwait(false);

            SourceMessages.Remove(this);
        }
    }
}