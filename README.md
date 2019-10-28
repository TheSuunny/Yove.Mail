# Yove.Mail - Email client for Temp-Mail.org

[![NuGet version](https://badge.fury.io/nu/Yove.Mail.svg)](https://badge.fury.io/nu/Yove.Mail)
[![Downloads](https://img.shields.io/nuget/dt/Yove.Mail.svg)](https://www.nuget.org/packages/Yove.Mail)
[![Target](https://img.shields.io/badge/.NET%20Standard-2.0-green.svg)](https://docs.microsoft.com/ru-ru/dotnet/standard/net-standard)

<a href="https://www.buymeacoffee.com/3ZEnINLSR" target="_blank"><img src="https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png" alt="Buy Me A Coffee" style="height: auto !important;width: auto !important;" ></a>

Nuget: https://www.nuget.org/packages/Yove.Mail/

```
Install-Package Yove.Mail
```

```
dotnet add package Yove.Mail
```

---

### Setup

```csharp
Email Mail = new Email
{
    Proxy = new ProxyClient("195.208.172.70", 8080, ProxyType.Http),
    Proxy = new ProxyClient("195.208.172.70", 8080, ProxyType.Socks4),
    Proxy = new ProxyClient("195.208.172.70", 8080, ProxyType.Socks5),
    Proxy = new ProxyClient("195.208.172.70:8080", ProxyType.Http)
}

string Domain = Mail.Domains[new Random().Next(0, Mail.Domains.Count - 1)];

string Address = await Mail.Set("yove", Domain); // Set email address
string Address = await Mail.SetRandom(); // Set random email address

Mail.NewMessage += async (e) =>
{
    Console.WriteLine($"{e.TextBody} / {e.HtmlBody} / {e.From} / {e.Subject} / {e.Date}");

    await e.Delete(); // Delete this message

    bool IsDelete = Mail.Dispose(); // Be sure to exit the client when you finish working with it
};

List<Message> Messages = Mail.Messages; // Return List messages from this Email

Message Message = Mail.GetMessage("Id Message"); // Return message from Id 0

await Mail.Delete(); // Delete email address
```

---

### Other

If you are missing something in the library, do not be afraid to write me :)

<thesunny@tuta.io>
