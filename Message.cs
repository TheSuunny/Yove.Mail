using System;
using System.Threading.Tasks;

namespace Yove.Mail
{
    public class Message : Settings
    {
        public string Id { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string Encoding { get; set; }
        public string Body { get; set; }

        public DateTime Date { get; set; }

        public async Task Delete()
        {
            await Client.Get($"https://temp-mail.org/en/delete/{Id}/").ConfigureAwait(false);

            SourceMessages.Remove(this);
        }
    }
}