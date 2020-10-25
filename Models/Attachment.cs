using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Yove.Http;
using static Yove.Mail.Settings;

namespace Yove.Mail.Models
{
    public class Attachment
    {
        public string Filename { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }
        public string Url { get; set; }

        public async Task<string> Download(string Directory)
        {
            HttpResponse Response = await Client.Get(Url);

            if (Response.Body.Contains("error"))
                throw new Exception((string)Response.Json["error"]);

            string FilePath = $"{Directory.TrimEnd('/')}/{(string)Response["name"]}";

            File.WriteAllBytes(FilePath, Convert.FromBase64String((string)Response["content"]));

            return FilePath;
        }
    }
}