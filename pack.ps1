#!/usr/bin/env bash

# Clean and build in release
dotnet restore /nowarn:netsdk1138
dotnet clean
dotnet build -c Release

# Create all NuGet packages

dotnet pack src/Indice.AspNetCore.Authentication.Apple/Indice.AspNetCore.Authentication.Apple.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.Authentication.GovGr/Indice.AspNetCore.Authentication.GovGr.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.EmbeddedUI/Indice.AspNetCore.EmbeddedUI.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.Identity/Indice.AspNetCore.Identity.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore/Indice.AspNetCore.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Common/Indice.Common.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.EntityFrameworkCore/Indice.EntityFrameworkCore.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Extensions.Configuration.Database/Indice.Extensions.Configuration.Database.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.AdminUI/Indice.Features.Identity.AdminUI.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.Server/Indice.Features.Identity.Server.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.UI/Indice.Features.Identity.UI.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.UI/Indice.Features.Identity.UI.Assets.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.Core/Indice.Features.Identity.Core.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.SignInLogs/Indice.Features.Identity.SignInLogs.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.AspNetCore/Indice.Features.Cases.AspNetCore.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.UI/Indice.Features.Cases.UI.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.GovGr/Indice.Features.GovGr.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.AspNetCore/Indice.Features.Messages.AspNetCore.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Core/Indice.Features.Messages.Core.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.UI/Indice.Features.Messages.UI.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Worker.Azure/Indice.Features.Messages.Worker.Azure.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Worker/Indice.Features.Messages.Worker.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.AspNetCore/Indice.Features.Multitenancy.AspNetCore.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.Core/Indice.Features.Multitenancy.Core.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.Worker.Azure/Indice.Features.Multitenancy.Worker.Azure.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Hosting/Indice.Hosting.csproj --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Services/Indice.Services.csproj --no-build -c Release -o ./artifacts