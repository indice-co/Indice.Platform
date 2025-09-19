# Indice.Features.FtpServer

## Instructions

To use the FTP server feature in your ASP.NET Core application or .NET Core Worker , follow these steps:

1. Install the `Indice.Features.FtpServer` NuGet package
1. Edit your `Program.cs` file to include the FTP server configuration
1. Configure the FTP server options as needed. Select atlease one of `UserDotNetFileSystem` or `UseAzureBlobFileSystem`

```csharp
using FubarDev.FtpServer;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddFtpServer(ftp => ftp
       /* example of using .NET file system
       .UseDotNetFileSystem(options => { 
            options.RootPath = builder.Environment.ContentRootPath + "/App_Data";
       })
       */
       // using Azure Blob Storage as file system
       .UseAzureBlobFileSystem(options => {
           options.ConnectrionString = builder.Configuration.GetConnectionString("StorageConnection");
       })
       // each user has its own root directory based on their email (password when anonymous is selected)
       .UseRootPerUser(options => {
           options.AnonymousRootPerEmail = true;
       })
       .EnableAnonymousAuthentication()
       // run as a hosted service and configure the general ftp server settings such as port etc.
       .RunAsHostedService(opt => {
           opt.Port = 2121;
       }));

var host = builder.Build();
host.Run();

```

The extension is based on [FubarDev.FtpServer](https://fubardevelopment.github.io/FtpServer/).