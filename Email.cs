using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Yove.Http;
using Yove.Http.Proxy;

namespace Yove.Mail
{
    //TODO: Proxy Client

    public delegate void EmailAction(Message Message);

    public class Email : Settings
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
            await Client.Get("https://temp-mail.org/en/option/delete/");

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

            Token = new CancellationTokenSource();

            if (Proxy != null)
                Client.Proxy = Proxy;

            HttpResponse Change = await Client.Post("https://temp-mail.org/en/option/change/", $"csrf={CSRFToken}&mail={Login}&domain={Domain}", "application/x-www-form-urlencoded").ConfigureAwait(false);

            if (Change.StatusCode == HttpStatusCode.OK)
            {
                new Task(async() => await Refresh()).Start();

                return HttpUtils.Parser("class=\"emailbox-input opentip\" value=\"", Change.Body, "\"");
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

                    foreach (string SourceLink in HttpUtils.Parser("<div class=\"inbox-dataList\">", Source, "<div class=\"mid-intro-text\">").Split(new string[] { "<a href=\"" }, StringSplitOptions.None))
                    {
                        try
                        {
                            if (!SourceLink.Contains("https://"))
                                continue;

                            string Id = HttpUtils.Parser("https://temp-mail.org/en/view/", SourceLink, "\" title");

                            if (Messages.FirstOrDefault(x => x.Id == Id) == null && !string.IsNullOrEmpty(Id))
                            {
                                string SourceMessage = await Client.GetString($"https://temp-mail.org/en/source/{Id}/").ConfigureAwait(false);

                                string From = HttpUtils.Parser("From:  <", SourceMessage, ">");
                                string Subject = HttpUtils.Parser("Subject: ", SourceMessage, "\r\n");
                                string ContentEncoding = HttpUtils.Parser("Content-Transfer-Encoding: ", SourceMessage, "\r\n");
                                string Body = HttpUtils.Parser($"{ContentEncoding}\r\n\r\n", SourceMessage, "\r\n");
                                string Date = HttpUtils.Parser($"Date: ", SourceMessage, " (");

                                if (ContentEncoding == "base64")
                                    Body = Encoding.UTF8.GetString(Convert.FromBase64String(Body));
                                else if (ContentEncoding == "quoted-printable")
                                    Body = DecodeQuotedPrintable(Body, Encoding.UTF8);

                                if (Subject.Contains("=?UTF-8"))
                                    Subject = DecodeMime(Subject);

                                Message Message = new Message
                                {
                                    Id = Id,
                                    From = From,
                                    Subject = Subject,
                                    Encoding = ContentEncoding,
                                    Body = Body,
                                    Date = TimeZoneInfo.ConvertTime(DateTime.Parse(Date), TimeZoneInfo.Local)
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
        }

        private string DecodeMime(string Source)
        {
            MatchCollection Match = Regex.Matches(Source, @"(?:=\?)([^\?]+)(?:\?B\?)([^\?]*)(?:\?=)");

            string Charset = Match[0].Groups[1].Value;
            string Data = Match[0].Groups[2].Value;

            return Encoding.GetEncoding(Charset).GetString(Convert.FromBase64String(Data));
        }

        private string DecodeQuotedPrintable(string Source, Encoding Encoding)
        {
            Source = Source.Replace("=\r\n", "");

            List<byte> Result = new List<byte>();

            for (int i = 0; i < Source.Length; i++)
            {
                if (Source[i] == '=')
                {
                    Result.Add(Convert.ToByte(Source.Substring(i + 1, 2), 16));
                    i += 2;
                }
                else
                {
                    Result.Add((byte)Source[i]);
                }
            }

            return Encoding.GetString(Result.ToArray());
        }
    }
}