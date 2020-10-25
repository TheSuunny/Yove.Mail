using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Yove.Http.Proxy;
using System.Security.Cryptography;
using System.Text;
using Yove.Http;
using Yove.Mail.Models;

using static Yove.Mail.Settings;

namespace Yove.Mail
{
    public delegate void EmailAction(Message Message);

    public class TempMail : IDisposable
    {
        public event EmailAction NewMessage;

        public List<string> Domains = new List<string>();

        public List<Message> Messages
        {
            get
            {
                return AllMessages;
            }
        }

        public string Address { get; set; }
        public ProxyClient Proxy { get; set; }
        public bool IsDisposed { get; private set; }

        public TempMail()
        {
            JToken Response = Client.GetJson("https://api4.temp-mail.org/request/domains/format/json").GetAwaiter().GetResult();

            foreach (JValue Domain in Response)
                Domains.Add((string)Domain);
        }

        public string Set(string Login, string Domain)
        {
            if (string.IsNullOrEmpty(Login) || !Domain.Contains("@"))
                throw new ArgumentException("Email invalid.");

            Address = $"{Login.ToLower()}{Domain}";
            Hash = CreateMD5(Address);

            WaitMessage();

            return Address;
        }

        public string SetRandom()
        {
            Address = $"{HttpUtils.RandomString(10).ToLower()}{Domains[new Random().Next(0, Domains.Count - 1)]}";
            Hash = CreateMD5(Address);

            WaitMessage();

            return Address;
        }

        private async void WaitMessage()
        {
            while (!IsDisposed)
            {
                try
                {
                    HttpResponse Response = await Client.Get($"https://api4.temp-mail.org/request/mail/id/{Hash}/format/json");

                    if (Response.Body.Contains("There are no emails yet"))
                        continue;

                    foreach (JObject Message in Response.Json)
                    {
                        if (Messages.FirstOrDefault(x => x.Id == (string)Message["mail_id"]) != null)
                            continue;

                        Message Msg = new Message
                        {
                            Id = (string)Message["mail_id"],
                            From = (string)Message["mail_from"],
                            Subject = (string)Message["mail_subject"],
                            TextBody = (string)Message["mail_text"],
                            HtmlBody = (string)Message["mail_html"],
                            Date = new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds((double)Message["mail_timestamp"])
                        };

                        foreach (JObject File in Message["mail_attachments"])
                        {
                            Msg.Attachments.Add(new Attachment
                            {
                                Filename = (string)File["filename"],
                                Size = (long)File["size"],
                                MimeType = (string)File["mimetype"],
                                Url = $"https://api4.temp-mail.org/request/one_attachment/id/{Msg.Id}/{(int)File["_id"]}/format/json"
                            });
                        }

                        if (NewMessage != null && Msg.Date.AddMinutes(2) > DateTime.UtcNow)
                            NewMessage(Msg);

                        Messages.Add(Msg);
                    }
                }
                catch
                {
                    //? Ignore
                }
                finally
                {
                    await Task.Delay(5000);
                }
            }
        }

        public Message GetMessage(string Id)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("This object disposed.");

            return Messages.FirstOrDefault(x => x.Id == Id);
        }

        public async Task Delete()
        {
            await Client.Get($"https://api4.temp-mail.org/request/delete_address/id/{Hash}/format/json");

            Dispose();
        }

        public static string CreateMD5(string Input)
        {
            using (MD5 MD5 = MD5.Create())
            {
                byte[] HashBytes = MD5.ComputeHash(Encoding.ASCII.GetBytes(Input));

                StringBuilder Builder = new StringBuilder();

                for (int i = 0; i < HashBytes.Length; i++)
                    Builder.Append(HashBytes[i].ToString("X2"));

                return Builder.ToString().ToLower();
            }
        }

        public void Dispose()
        {
            Client.Dispose();

            Domains.Clear();
            AllMessages.Clear();

            IsDisposed = true;
        }

        ~TempMail()
        {
            Dispose();

            GC.SuppressFinalize(this);
        }
    }
}