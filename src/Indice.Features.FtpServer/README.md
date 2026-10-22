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

## Health checks & capacity probing

You can expose the FTP server readiness and remaining capacity over HTTP. The probe reports active control connections, active data connections, and free passive (PASV) ports. To do so, enable connection tracking on the FTP server and register the health check.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFtpServer(ftp => ftp
       .UseAzureBlobFileSystem(options => { /* ... */ })
       .EnableAnonymousAuthentication(options => options.AllowAnyPassword = true)
       // every concurrent passive transfer needs one port from this range
       .UsePassiveAddressResolution(options => {
           options.PasvMinPort = 50000;
           options.PasvMaxPort = 50099;
       })
       // tracks PASV listeners & data connections (call inside AddFtpServer)
       .EnableConnectionTracking()
       .RunAsHostedService(opt => {
           opt.Port = 2121;
           opt.MaxActiveConnections = 200; // 0 = unlimited
       }));

builder.Services.AddHealthChecks()
       .AddFtpServer(name: "ftp", configure: options => {
           options.DegradedFreeCapacityRatio = 0.1;     // degraded when <= 10% capacity is free
           options.InspectOperatingSystemPorts = false; // also check the OS TCP table for the passive port range
       });

var app = builder.Build();

app.MapHealthChecks("/health/ftp");
// raw numbers, e.g. for a load balancer / scheduler that needs to know how many new transfers it can start
app.MapGet("/ftp/capacity", (IFtpServerCapacityProvider capacity) => capacity.GetCapacity());

app.Run();
```

The health check reports:

- `Unhealthy` when the server is not ready to accept connections.
- `Degraded` when free control connections or free passive ports drop to or below `DegradedFreeCapacityRatio`. With the default of `0`, that means fully exhausted.
- `Healthy` otherwise.

Every result carries the following data, also available through `IFtpServerCapacityProvider.GetCapacity()`:

| Key | Description |
|---|---|
| `controlConnectionsActive` / `controlConnectionsMax` / `controlConnectionsFree` | FTP control connections (limit from `MaxActiveConnections`). |
| `dataConnectionsActive` / `dataConnectionsTotal` | Open data connections (active & passive mode). |
| `passiveListenersPending` | `PASV` issued but the client has not connected yet. |
| `passivePortsCapacity` / `passivePortsInUse` / `passivePortsFree` | Usage of the passive port range. |
| `passiveListenerFailures` | Number of `PASV` requests that failed (e.g. port range exhausted). |
| `available` | How many new transfers can be started right now (min of free control connections and free passive ports). |

> Counts are per server instance. When running multiple replicas behind a load balancer, probe each instance.

The extension is based on [FubarDev.FtpServer](https://fubardevelopment.github.io/FtpServer/).