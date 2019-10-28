using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Yove.Http.Proxy;
using System.Security.Cryptography;
using System.Text;
using Yove.Http;

namespace Yove.Mail
{
    public delegate void EmailAction(Message Message);

    public class TempMail : Settings, IDisposable
    {
        public event EmailAction NewMessage;

        public List<string> Domains = new List<string>();

        public List<Message> Messages => SourceMessages;

        public string Address { get; set; }

        public ProxyClient Proxy { get; set; }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            Client.Dispose();

            Domains.Clear();
            Messages.Clear();

            IsDisposed = true;
        }

        public TempMail()
        {
            string Response = Client.GetString("https://api4.temp-mail.org/request/domains/format/json").GetAwaiter().GetResult();

            foreach (var Domain in JArray.Parse(Response))
                Domains.Add((string)Domain);
        }

        public string Set(string Login, string Domain)
        {
            if (string.IsNullOrEmpty(Login) || !Domain.Contains("@"))
                throw new ArgumentException("Email invalid.");

            Address = $"{Login.ToLower()}{Domain}";

            Hash = CreateMD5(Address);

            new Task(async () => await WaitMessage()).Start();

            return Address;
        }

        public string SetRandom()
        {
            Address = $"{HttpUtils.RandomString(10).ToLower()}{Domains[new Random().Next(0, Domains.Count - 1)]}";

            Hash = CreateMD5(Address);

            new Task(async () => await WaitMessage()).Start();

            return Address;
        }

        private async Task WaitMessage()
        {
            while (!IsDisposed)
            {
                try
                {
                    string GetMessages = await Client.GetString($"https://api4.temp-mail.org/request/mail/id/{Hash}/format/json");

                    if (!GetMessages.Contains("There are no emails yet"))
                    {
                        foreach (var Message in JArray.Parse(GetMessages))
                        {
                            if (Messages.FirstOrDefault(x => x.Id == (string)Message["mail_id"]) != null)
                                continue;

                            DateTime Date = new DateTime(1970, 1, 1, 0, 0, 0, 0);

                            Message Msg = new Message
                            {
                                Id = (string)Message["mail_id"],
                                From = (string)Message["mail_from"],
                                Subject = (string)Message["mail_subject"],
                                TextBody = (string)Message["mail_text"],
                                HtmlBody = (string)Message["mail_html"],
                                Date = Date.AddSeconds((double)Message["mail_timestamp"])
                            };

                            foreach (var File in Message["mail_attachments"])
                            {
                                Msg.Attachments.Add(((string)Message["filename"], (long)Message["size"], (string)Message["mimetype"], $"https://api4.temp-mail.org/request/one_attachment/id/{Hash}/{(int)File["_id"]}/format/json"));
                            }

                            if (NewMessage != null && Msg.Date.AddMinutes(2) > DateTime.UtcNow)
                                NewMessage(Msg);

                            Messages.Add(Msg);
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

        public Message GetMessage(string Id)
        {
            if (IsDisposed)
                throw new ObjectDisposedException("This object disposed.");

            Message Message = Messages.FirstOrDefault(x => x.Id == Id);

            if (Messages.Count == 0 || Message == null)
                return null;

            return Message;
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
    }
}