using System.Security.Claims;
using System.Text;
using Indice.Events;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Core.Services.CaseMessageService;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Indice.Features.Cases.Tests;

public class CaseAttachmentFileServiceTests
{
    private static ServiceProvider BuildServiceProvider(bool useFileService) {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection()
            .AddDbContext<CasesDbContext>(builder => builder.UseInMemoryDatabase(databaseName))
            .Configure<CasesOptions>(options => { });
        services.AddSingleton(Substitute.For<IPlatformEventService>());
        services.AddSingleton(Substitute.For<ISchemaValidator>());
        services.AddSingleton(Substitute.For<ICaseAuthorizationProvider>());
        services.AddTransient<IAdminCaseMessageService, AdminCaseMessageService>();
        services.AddTransient<IAdminCaseService, AdminCaseService>();
        if (useFileService) {
            services.AddFiles(builder => builder.AddFilesInMemory(CasesCoreConstants.FileServiceKey));
        }
        return services.BuildServiceProvider();
    }

    private static async Task<DbCase> SeedCase(IServiceProvider serviceProvider) {
        var dbContext = serviceProvider.GetRequiredService<CasesDbContext>();
        var caseType = new DbCaseType {
            Id = Guid.NewGuid(),
            Title = "Applications",
            Code = "APPL",
            DataSchema = "{}"
        };
        var @case = new DbCase {
            CaseTypeId = caseType.Id,
            Channel = "web",
            CreatedBy = AuditMeta.Create(Actor())
        };
        dbContext.CaseTypes.Add(caseType);
        dbContext.Cases.Add(@case);
        await dbContext.SaveChangesAsync();
        return @case;
    }

    private static UserActor Actor() => new ClaimsPrincipal(new ClaimsIdentity([
        new Claim("sub", "ab9769f1-d532-4b7d-9922-3da003157ebd"),
        new Claim("name", "tester"),
        new Claim("email", "tester@example.com")
    ], "test", "name", "role")).UserToActor(new CasesOptions());

    private static async Task<Guid> SendAttachment(IServiceProvider serviceProvider, Guid caseId, byte[] content) {
        var messageService = serviceProvider.GetRequiredService<IAdminCaseMessageService>();
        var attachmentId = await messageService.Send(caseId, Actor(), new Message {
            FileName = "document.txt",
            FileStreamAccessor = () => new MemoryStream(content)
        }, AuditMeta.Create(Actor()));
        if (attachmentId is null) {
            throw new InvalidOperationException("Expected attachment id to be returned, but got null.");
        }
        return attachmentId.Value;
    }

    [Fact]
    public async Task Attachment_Is_Stored_In_Database_When_No_FileService_Is_Configured() {
        await using var serviceProvider = BuildServiceProvider(useFileService: false);
        var @case = await SeedCase(serviceProvider);
        var content = Encoding.UTF8.GetBytes("hello cases");

        var attachmentId = await SendAttachment(serviceProvider, @case.Id, content);

        var dbAttachment = await serviceProvider.GetRequiredService<CasesDbContext>().Attachments.AsNoTracking().SingleAsync(x => x.Id == attachmentId, TestContext.Current.CancellationToken);
        Assert.Equal(content, dbAttachment.Data);
        var attachment = await serviceProvider.GetRequiredService<IAdminCaseService>().GetAttachment(@case.Id, attachmentId);
        Assert.NotNull(attachment);
        Assert.Equal(content, attachment.Data);
    }

    [Fact]
    public async Task Attachment_Is_Stored_In_FileService_When_Configured() {
        await using var serviceProvider = BuildServiceProvider(useFileService: true);
        var @case = await SeedCase(serviceProvider);
        var content = Encoding.UTF8.GetBytes("hello cases");

        var attachmentId = await SendAttachment(serviceProvider, @case.Id, content);

        var dbAttachment = await serviceProvider.GetRequiredService<CasesDbContext>().Attachments.AsNoTracking().SingleAsync(x => x.Id == attachmentId, TestContext.Current.CancellationToken);
        Assert.Null(dbAttachment.Data);
        Assert.Equal(content.Length, dbAttachment.ContentLength);
        var fileService = serviceProvider.GetRequiredKeyedService<IFileService>(CasesCoreConstants.FileServiceKey);
        Assert.Equal(content, await fileService.GetAsync(dbAttachment.GetPath()));

        var adminCaseService = serviceProvider.GetRequiredService<IAdminCaseService>();
        var attachment = await adminCaseService.GetAttachment(@case.Id, attachmentId);
        Assert.NotNull(attachment);
        Assert.Equal(content, attachment.Data);
        var attachmentById = await adminCaseService.GetAttachmentById(attachmentId);
        Assert.NotNull(attachmentById);
        Assert.Equal(content, attachmentById.Data);
    }

    [Fact]
    public async Task Attachment_Stored_In_Database_Is_Still_Served_When_FileService_Is_Configured() {
        await using var serviceProvider = BuildServiceProvider(useFileService: true);
        var @case = await SeedCase(serviceProvider);
        var content = Encoding.UTF8.GetBytes("legacy attachment");
        var dbContext = serviceProvider.GetRequiredService<CasesDbContext>();
        var legacy = new DbAttachment(@case.Id);
        using var stream = new MemoryStream(content);
        legacy.PopulateFrom("legacy.txt", stream, saveData: true);
        dbContext.Attachments.Add(legacy);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var attachment = await serviceProvider.GetRequiredService<IAdminCaseService>().GetAttachment(@case.Id, legacy.Id);

        Assert.NotNull(attachment);
        Assert.Equal(content, attachment.Data);
    }

    [Fact]
    public void UseFileService_Extensions_Register_Keyed_FileService() {
        var services = new ServiceCollection();
        var options = new CasesOptions(services);
        options.UseFileServiceAzure();
        Assert.Contains(services, x => x.IsKeyedService && Equals(x.ServiceKey, CasesCoreConstants.FileServiceKey) && x.ServiceType == typeof(IFileService));

        services = new ServiceCollection();
        options = new CasesOptions(services);
        options.UseFileServiceLocal();
        Assert.Contains(services, x => x.IsKeyedService && Equals(x.ServiceKey, CasesCoreConstants.FileServiceKey) && x.ServiceType == typeof(IFileService));
    }
}
