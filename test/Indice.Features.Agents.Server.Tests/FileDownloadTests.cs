using System.Security.Claims;
using System.Text;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Services;
using Indice.Features.Agents.Server.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Server.Tests;

public class FileDownloadTests : IDisposable
{
    private readonly AgentsDbContext _db = new(new DbContextOptionsBuilder<AgentsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly ConversationFileStore _store;

    private static readonly Guid Chat = Guid.NewGuid();

    public FileDownloadTests() {
        _store = new ConversationFileStore(_db);
    }

    public void Dispose() => _db.Dispose();

    private static ClaimsPrincipal User(string? subject) =>
        new(new ClaimsIdentity(subject is null ? [] : [new Claim("sub", subject)], authenticationType: "test"));

    [Fact]
    public async Task The_Owner_Downloads_The_File_As_An_Attachment_That_Is_Never_Rendered_In_The_App_Origin() {
        var saved = await _store.SaveAsync(Chat, "user-1", "report.html", "text/html", Encoding.UTF8.GetBytes("<script>alert(1)</script>"), TestContext.Current.CancellationToken);
        var http = new DefaultHttpContext();

        var result = await MyChatsHandlers.DownloadFile(Chat, saved.Id, User("user-1"), _store, http, TestContext.Current.CancellationToken);

        var file = Assert.IsType<FileContentHttpResult>(result.Result);
        Assert.Equal("report.html", file.FileDownloadName); // a file download name makes the response an attachment
        Assert.Equal("text/html", file.ContentType);
        Assert.Equal("<script>alert(1)</script>", Encoding.UTF8.GetString(file.FileContents.Span));
        Assert.Equal("nosniff", http.Response.Headers.XContentTypeOptions.ToString());
        Assert.Equal("private, no-store", http.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task Another_User_Gets_A_404() {
        var saved = await _store.SaveAsync(Chat, "user-1", "report.md", "text/markdown", Encoding.UTF8.GetBytes("# report"), TestContext.Current.CancellationToken);

        var result = await MyChatsHandlers.DownloadFile(Chat, saved.Id, User("user-2"), _store, new DefaultHttpContext(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task A_Caller_Without_A_Subject_Gets_A_404() {
        var saved = await _store.SaveAsync(Chat, "user-1", "report.md", "text/markdown", Encoding.UTF8.GetBytes("# report"), TestContext.Current.CancellationToken);

        var result = await MyChatsHandlers.DownloadFile(Chat, saved.Id, User(null), _store, new DefaultHttpContext(), TestContext.Current.CancellationToken);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task An_Unknown_File_Or_Chat_Is_A_404_Without_Setting_Download_Headers() {
        var http = new DefaultHttpContext();

        var result = await MyChatsHandlers.DownloadFile(Chat, Guid.NewGuid(), User("user-1"), _store, http, TestContext.Current.CancellationToken);

        Assert.IsType<NotFound>(result.Result);
        Assert.False(http.Response.Headers.ContainsKey("X-Content-Type-Options"));
    }
}
