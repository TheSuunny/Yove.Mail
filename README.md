# Yove.Mail - Email client for Temp-Mail.org

[![NuGet version](https://badge.fury.io/nu/Yove.Mail.svg)](https://badge.fury.io/nu/Yove.Mail)
[![Downloads](https://img.shields.io/nuget/dt/Yove.Mail.svg)](https://www.nuget.org/packages/Yove.Mail)
[![Target](https://img.shields.io/badge/.NET%20Standard-2.0-green.svg)](https://docs.microsoft.com/ru-ru/dotnet/standard/net-standard)

Nuget: https://www.nuget.org/packages/Yove.Mail/

```
Install-Package Yove.Mail
```

```
dotnet add package Yove.Mail
```
___

### Setup

```csharp
Email Mail = new Email();

Mail.Domains // Return List domains

string Address = await Mail.Set("yove", "@dreamcatcher.email"); // Set mail address

Mail.NewMessage += async (e) =>
{
    Console.WriteLine($"{e.Body} / {e.From} / {e.Subject} / {e.Date}");

    await e.Delete(); // Delete this message

    Dispose(); // Be sure to exit the client when you finish working with it
};

Mail.Messages // Return List messages from this Email

Message Message = Mail.GetMessage(0); // Return message from Id 0
```

___

### Other

If you are missing something in the library, do not be afraid to write me :)

<yove@keemail.me>