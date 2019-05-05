using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yove.Http;
using Yove.Http.Proxy;
using MimeKit;

namespace Yove.Mail
{
    public delegate void EmailAction(Message Message);

    public class Email : Settings, IDisposable
    {
        public event EmailAction NewMessage;

        public List<string> Domains = new List<string>();
        public List<Message> Messages => SourceMessages;

        public ProxyClient Proxy { get; set; }

        public bool IsDisposed { get; private set; }

        private CancellationTokenSource Token { get; set; }

        public Email()
        {
            GetDomains().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            Token.Cancel();
            Client.Dispose();

            Domains.Clear();
            Messages.Clear();

            IsDisposed = true;
        }

        public async Task Delete()
        {
            await Client.Get("https://temp-mail.org/en/option/delete/").ConfigureAwait(false);

            Token.Cancel();
            Messages.Clear();
        }

        public Message GetMessage(int Id)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("This object disposed");

            if (Messages.Count == 0 || Id > Messages.Count)
                return null;

            return Messages[Id];
        }

        public async Task<string> Set(string Login, string Domain)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("This object disposed");

            if (Proxy != null)
                Client.Proxy = Proxy;

            if (Token != null)
            {
                Token.Cancel();

                while (Token != null)
                    await Task.Delay(100).ConfigureAwait(false);
            }

            Token = new CancellationTokenSource();

            HttpResponse Change = await Client.Post("https://temp-mail.org/en/option/change/", $"csrf={CSRFToken}&mail={Login}&domain={Domain}", "application/x-www-form-urlencoded").ConfigureAwait(false);

            if (Change.StatusCode == HttpStatusCode.OK)
            {
                string Email = HttpUtils.Parser("class=\"emailbox-input opentip\" value=\"", Change.Body, "\"");

                if (Email != null)
                    new Task(async () => await Refresh()).Start();

                return Email;
            }

            return null;
        }

        private async Task<List<string>> GetDomains()
        {
            string Source = await Client.GetString("https://temp-mail.org/en/option/change/").ConfigureAwait(false);

            CSRFToken = HttpUtils.Parser("name=\"csrf\" value=\"", Source, "\"");

            foreach (string SourceDomain in HttpUtils.Parser("<select id=\"domain\" name=\"domain\"", Source, "</select>").Split(new string[] { "<option value=\"@" }, StringSplitOptions.None))
            {
                if (!SourceDomain.Contains("@"))
                    continue;

                string Domain = HttpUtils.Parser("\">", SourceDomain, "</option>")?.Trim();

                if (Domain != null)
                    Domains.Add(Domain);
            }

            return Domains;
        }

        private async Task Refresh()
        {
            while (!Token.IsCancellationRequested)
            {
                try
                {
                    string Source = await Client.GetString("https://temp-mail.org/en/option/refresh/").ConfigureAwait(false);

                    string Inbox = HttpUtils.Parser("<div class=\"inbox-dataList\">", Source, "<div class=\"mid-intro-text\">");

                    if (Inbox == null || !Inbox.Contains("<a href=\""))
                        continue;

                    foreach (string SourceLink in Inbox.Split(new string[] { "<a href=\"" }, StringSplitOptions.None))
                    {
                        try
                        {
                            if (!SourceLink.Contains("https://"))
                                continue;

                            string Id = HttpUtils.Parser("https://temp-mail.org/en/view/", SourceLink, "\" title");

                            if (Messages.FirstOrDefault(x => x.Id == Id) == null && !string.IsNullOrEmpty(Id))
                            {
                                MimeMessage Mime = await MimeMessage.LoadAsync(await Client.GetStream($"https://temp-mail.org/en/source/{Id}/")).ConfigureAwait(false);

                                Message Message = new Message
                                {
                                    Id = Id,
                                    From = Mime.From.First().Name,
                                    Subject = Mime.Subject,
                                    TextBody = Mime.TextBody,
                                    HtmlBody = Mime.HtmlBody,
                                    Date = TimeZoneInfo.ConvertTime(Mime.Date, TimeZoneInfo.Local)
                                };

                                if (NewMessage != null && Message.Date.AddMinutes(2) > DateTime.Now)
                                    NewMessage(Message);

                                Messages.Add(Message);
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }
                catch
                {
                    // Ignore
                }
                finally
                {
                    await Task.Delay(5000);
                }
            }

            Token = null;
        }
    }
}