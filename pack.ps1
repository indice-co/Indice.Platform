#!/usr/bin/env bash
# Create all NuGet packages

dotnet pack src/Indice.AspNetCore.Authentication.Apple/Indice.AspNetCore.Authentication.Apple.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.Authentication.GovGr/Indice.AspNetCore.Authentication.GovGr.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.EmbeddedUI/Indice.AspNetCore.EmbeddedUI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore/Indice.AspNetCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.AspNetCore.Builder/Indice.AspNetCore.Builder.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Common/Indice.Common.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.EntityFrameworkCore/Indice.EntityFrameworkCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Extensions.Configuration.Database/Indice.Extensions.Configuration.Database.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.AdminUI/Indice.Features.Identity.AdminUI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.Server/Indice.Features.Identity.Server.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.Core/Indice.Features.Identity.Core.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.UI/Indice.Features.Identity.UI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Identity.SignInLogs/Indice.Features.Identity.SignInLogs.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.Core/Indice.Features.Cases.Core.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.AspNetCore/Indice.Features.Cases.AspNetCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.Server/Indice.Features.Cases.Server.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Cases.UI/Indice.Features.Cases.UI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.GovGr/Indice.Features.GovGr.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Media.AspNetCore/Indice.Features.Media.AspNetCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.AspNetCore/Indice.Features.Messages.AspNetCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Core/Indice.Features.Messages.Core.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.UI/Indice.Features.Messages.UI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Worker.Azure/Indice.Features.Messages.Worker.Azure.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Messages.Worker/Indice.Features.Messages.Worker.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.AspNetCore/Indice.Features.Multitenancy.AspNetCore.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.Core/Indice.Features.Multitenancy.Core.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Multitenancy.Worker.Azure/Indice.Features.Multitenancy.Worker.Azure.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Risk.Core/Indice.Features.Risk.Core.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Risk.Server/Indice.Features.Risk.Server.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Features.Risk.UI/Indice.Features.Risk.UI.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Hosting/Indice.Hosting.csproj --no-restore --no-build -c Release -o ./artifacts
dotnet pack src/Indice.Services/Indice.Services.csproj --no-restore --no-build -c Release -o ./artifacts