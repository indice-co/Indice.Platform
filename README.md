[![nugget build](https://github.com/indice-co/Indice.Platform/actions/workflows/publish_to_nuget.yml/badge.svg)](https://github.com/indice-co/Indice.Platform/actions/workflows/publish_to_nuget.yml)
[![CodeQL](https://github.com/indice-co/Indice.Platform/actions/workflows/codeql.yml/badge.svg)](https://github.com/indice-co/Indice.Platform/security)
[![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore/)
[![OpenSSF Scorecard](https://api.scorecard.dev/projects/github.com/indice-co/Indice.Platform/badge)](https://scorecard.dev/viewer/?uri=github.com/indice-co/Indice.Platform)


# Indice.Platform ![alt text](icon/icon-64.png "Indice logo")
.Net addons and helpers for creating distributed web applications and services.

## Installation

To install any of the platform packages, run the dotnet add command. Or download one from our nuget.org [profile](https://www.nuget.org/profiles/indice)

## 1. NuGet Packages

### Core Framework
Foundation packages providing common functionality and utilities

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Common | Common utilities, extensions, and base classes | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Common?logo=nuget)](https://www.nuget.org/packages/Indice.Common/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Common)](https://www.nuget.org/packages/Indice.Common/) | n/a |
| Indice.AspNetCore | ASP.NET Core extensions and middleware | [![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.AspNetCore)](https://www.nuget.org/packages/Indice.AspNetCore/) | [CHANGELOG.md](src/Indice.AspNetCore/CHANGELOG.md) |
| Indice.EntityFrameworkCore | Entity Framework Core utilities and extensions | [![Nuget](https://img.shields.io/nuget/vpre/Indice.EntityFrameworkCore?logo=nuget)](https://www.nuget.org/packages/Indice.EntityFrameworkCore/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.EntityFrameworkCore)](https://www.nuget.org/packages/Indice.EntityFrameworkCore/) | n/a |
| Indice.AspNetCore.Builder | Fluent builders for ASP.NET Core configuration | [![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore.Builder?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore.Builder/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.AspNetCore.Builder)](https://www.nuget.org/packages/Indice.AspNetCore.Builder/) | n/a |
| Indice.AspNetCore.EmbeddedUI | Embedded UI components and resources | [![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore.EmbeddedUI?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore.EmbeddedUI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.AspNetCore.EmbeddedUI)](https://www.nuget.org/packages/Indice.AspNetCore.EmbeddedUI/) | n/a |
| Indice.Extensions.Configuration.Database | Database configuration provider | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Extensions.Configuration.Database?logo=nuget)](https://www.nuget.org/packages/Indice.Extensions.Configuration.Database/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Extensions.Configuration.Database)](https://www.nuget.org/packages/Indice.Extensions.Configuration.Database/) | n/a |
| Indice.Hosting | Hosting utilities and background services | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Hosting?logo=nuget)](https://www.nuget.org/packages/Indice.Hosting/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Hosting)](https://www.nuget.org/packages/Indice.Hosting/) | [CHANGELOG.md](src/Indice.Hosting/CHANGELOG.md) |
| Indice.Services | Common services and implementations | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Services?logo=nuget)](https://www.nuget.org/packages/Indice.Services/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Services)](https://www.nuget.org/packages/Indice.Services/) | n/a |
| Indice.Functions.Builder | Azure Functions builder utilities | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Functions.Builder?logo=nuget)](https://www.nuget.org/packages/Indice.Functions.Builder/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Functions.Builder)](https://www.nuget.org/packages/Indice.Functions.Builder/) | n/a |

### Authentication & Authorization
Identity management, authentication providers, and authorization features

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Identity.Core | Core identity management functionality | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Identity.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Identity.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Identity.Core)](https://www.nuget.org/packages/Indice.Features.Identity.Core/) | [CHANGELOG.md](src/Indice.Features.Identity.Core/CHANGELOG.md) |
| Indice.Features.Identity.Server | Identity server implementation | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Identity.Server?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Identity.Server/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Identity.Server)](https://www.nuget.org/packages/Indice.Features.Identity.Server/) | [CHANGELOG.md](src/Indice.Features.Identity.Server/CHANGELOG.md) |
| Indice.Features.Identity.AdminUI | Administrative UI for identity management | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Identity.AdminUI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Identity.AdminUI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Identity.AdminUI)](https://www.nuget.org/packages/Indice.Features.Identity.AdminUI/) | n/a |
| Indice.Features.Identity.UI | End-user identity UI components | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Identity.UI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Identity.UI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Identity.UI)](https://www.nuget.org/packages/Indice.Features.Identity.UI/) | n/a |
| Indice.Features.Identity.SignInLogs | Sign-in logging and monitoring | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Identity.SignInLogs?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Identity.SignInLogs/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Identity.SignInLogs)](https://www.nuget.org/packages/Indice.Features.Identity.SignInLogs/) | [CHANGELOG.md](src/Indice.Features.Identity.SignInLogs/CHANGELOG.md) |
| Indice.Features.ActivityLogs | Activity logging and audit trail tracking | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.ActivityLogs?logo=nuget)](https://www.nuget.org/packages/Indice.Features.ActivityLogs/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.ActivityLogs)](https://www.nuget.org/packages/Indice.Features.ActivityLogs/) | n/a |
| Indice.AspNetCore.Authentication.Apple | Apple authentication provider | [![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore.Authentication.Apple?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore.Authentication.Apple/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.AspNetCore.Authentication.Apple)](https://www.nuget.org/packages/Indice.AspNetCore.Authentication.Apple/) | n/a |
| Indice.AspNetCore.Authentication.GovGr | Greek government authentication provider | [![Nuget](https://img.shields.io/nuget/vpre/Indice.AspNetCore.Authentication.GovGr?logo=nuget)](https://www.nuget.org/packages/Indice.AspNetCore.Authentication.GovGr/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.AspNetCore.Authentication.GovGr)](https://www.nuget.org/packages/Indice.AspNetCore.Authentication.GovGr/) | n/a |

### Messaging Platform
Comprehensive messaging system with campaign management and delivery

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Messages.Core | Core messaging functionality | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Messages.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Messages.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Messages.Core)](https://www.nuget.org/packages/Indice.Features.Messages.Core/) | [CHANGELOG.md](src/Indice.Features.Messages.Core/CHANGELOG.md) |
| Indice.Features.Messages.AspNetCore | ASP.NET Core messaging integration | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Messages.AspNetCore?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Messages.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Messages.AspNetCore)](https://www.nuget.org/packages/Indice.Features.Messages.AspNetCore/) | [CHANGELOG.md](src/Indice.Features.Messages.AspNetCore/CHANGELOG.md) |
| Indice.Features.Messages.UI | Messaging management UI | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Messages.UI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Messages.UI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Messages.UI)](https://www.nuget.org/packages/Indice.Features.Messages.UI/) | [CHANGELOG.md](src/Indice.Features.Messages.UI/CHANGELOG.md) |
| Indice.Features.Messages.Worker | Background message processing | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Messages.Worker?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Messages.Worker/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Messages.Worker)](https://www.nuget.org/packages/Indice.Features.Messages.Worker/) | n/a |
| Indice.Features.Messages.Worker.Azure | Azure-specific message workers | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Messages.Worker.Azure?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Messages.Worker.Azure/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Messages.Worker.Azure)](https://www.nuget.org/packages/Indice.Features.Messages.Worker.Azure/) | n/a |

### Agents
AI agents platform with core workflows, server APIs, and UI components

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Agents.Core | Core agents functionality and workflows | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Agents.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Agents.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Agents.Core)](https://www.nuget.org/packages/Indice.Features.Agents.Core/) | n/a |
| Indice.Features.Agents.Server | Agents server implementation and APIs | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Agents.Server?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Agents.Server/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Agents.Server)](https://www.nuget.org/packages/Indice.Features.Agents.Server/) | n/a |
| Indice.Features.Agents.UI | Agents user interface components | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Agents.UI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Agents.UI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Agents.UI)](https://www.nuget.org/packages/Indice.Features.Agents.UI/) | n/a |

### Case Management
Business process and case management system

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Cases.Core | Core case management functionality | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Cases.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Cases.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Cases.Core)](https://www.nuget.org/packages/Indice.Features.Cases.Core/) | [CHANGELOG.md](src/Indice.Features.Cases.Core/CHANGELOG.md) |
| Indice.Features.Cases.Server | Case management server implementation | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Cases.Server?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Cases.Server/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Cases.Server)](https://www.nuget.org/packages/Indice.Features.Cases.Server/) | n/a |
| Indice.Features.Cases.UI | Case management user interface | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Cases.UI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Cases.UI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Cases.UI)](https://www.nuget.org/packages/Indice.Features.Cases.UI/) | [CHANGELOG.md](src/Indice.Features.Cases.UI/CHANGELOG.md) |
| Indice.Features.Cases.Workflows | Workflow engine for case processing | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Cases.Workflows?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Cases.Workflows/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Cases.Workflows)](https://www.nuget.org/packages/Indice.Features.Cases.Workflows/) | n/a |

### Risk Management
Risk assessment and fraud detection capabilities

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Risk.Core | Core risk management functionality | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Risk.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Risk.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Risk.Core)](https://www.nuget.org/packages/Indice.Features.Risk.Core/) | [CHANGELOG.md](src/Indice.Features.Risk.Core/CHANGELOG.md) |
| Indice.Features.Risk.Server | Risk assessment server | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Risk.Server?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Risk.Server/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Risk.Server)](https://www.nuget.org/packages/Indice.Features.Risk.Server/) | n/a |
| Indice.Features.Risk.UI | Risk management user interface | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Risk.UI?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Risk.UI/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Risk.UI)](https://www.nuget.org/packages/Indice.Features.Risk.UI/) | n/a |

### Multitenancy
Multi-tenant application support and tenant management

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Multitenancy.Core | Core multitenancy functionality | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Multitenancy.Core?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Multitenancy.Core/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Multitenancy.Core)](https://www.nuget.org/packages/Indice.Features.Multitenancy.Core/) | n/a |
| Indice.Features.Multitenancy.AspNetCore | ASP.NET Core multitenancy integration | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Multitenancy.AspNetCore?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Multitenancy.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Multitenancy.AspNetCore)](https://www.nuget.org/packages/Indice.Features.Multitenancy.AspNetCore/) | n/a |
| Indice.Features.Multitenancy.Worker.Azure | Azure-specific tenant workers | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Multitenancy.Worker.Azure?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Multitenancy.Worker.Azure/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Multitenancy.Worker.Azure)](https://www.nuget.org/packages/Indice.Features.Multitenancy.Worker.Azure/) | n/a |

### Specialized Features
Additional specialized functionality and integrations

| Package | Description | Version | Downloads | CHANGELOG |
|---------|-------------|---------|-----------|-----------|
| Indice.Features.Media.AspNetCore | Media management and processing | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.Media.AspNetCore?logo=nuget)](https://www.nuget.org/packages/Indice.Features.Media.AspNetCore/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.Media.AspNetCore)](https://www.nuget.org/packages/Indice.Features.Media.AspNetCore/) | [CHANGELOG.md](src/Indice.Features.Media.AspNetCore/CHANGELOG.md) |
| Indice.Features.GovGr | Greek government services integration | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.GovGr?logo=nuget)](https://www.nuget.org/packages/Indice.Features.GovGr/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.GovGr)](https://www.nuget.org/packages/Indice.Features.GovGr/) | [CHANGELOG.md](src/Indice.Features.GovGr/CHANGELOG.md) |
| Indice.Features.GeoIP | IP geolocation services | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.GeoIP?logo=nuget)](https://www.nuget.org/packages/Indice.Features.GeoIP/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.GeoIP)](https://www.nuget.org/packages/Indice.Features.GeoIP/) | n/a |
| Indice.Features.FtpServer | FTP server implementation | [![Nuget](https://img.shields.io/nuget/vpre/Indice.Features.FtpServer?logo=nuget)](https://www.nuget.org/packages/Indice.Features.FtpServer/) | [![Nuget](https://img.shields.io/nuget/dt/Indice.Features.FtpServer)](https://www.nuget.org/packages/Indice.Features.FtpServer/) | n/a |