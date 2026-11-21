using System.Security.Cryptography;
using System.Text;
using Indice.Features.Agents.Core.Data;
using Indice.Features.Agents.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Agents.Core.Tests;

public class ConversationFileStoreTests : IDisposable
{
    private readonly AgentsDbContext _db = new(new DbContextOptionsBuilder<AgentsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly ConversationFileStore _store;

    private static readonly Guid Conversation = Guid.NewGuid();
    private const string Owner = "user-1";

    public ConversationFileStoreTests() {
        _store = new ConversationFileStore(_db);
    }

    public void Dispose() => _db.Dispose();

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    [Fact]
    public async Task A_Saved_File_Round_Trips_With_Its_Hash_And_Size() {
        var content = Bytes("<html>report</html>");

        var saved = await _store.SaveAsync(Conversation, Owner, "report.html", "text/html", content, TestContext.Current.CancellationToken);
        var read = await _store.GetAsync(Conversation, saved.Id, Owner, TestContext.Current.CancellationToken);

        Assert.NotNull(read);
        Assert.Equal(content, read.Data);
        Assert.Equal("report.html", read.Reference.FileName);
        Assert.Equal("text/html", read.Reference.ContentType);
        Assert.Equal(content.Length, read.Reference.Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), read.Reference.Sha256);
        Assert.Equal(saved.Sha256, read.Reference.Sha256);
        Assert.Equal(DateTimeOffset.UtcNow, read.CreatedAt, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Another_User_Gets_Nothing_Exactly_As_For_A_Missing_File() {
        var saved = await _store.SaveAsync(Conversation, Owner, "report.md", "text/markdown", Bytes("# report"), TestContext.Current.CancellationToken);

        Assert.Null(await _store.GetAsync(Conversation, saved.Id, "someone-else", TestContext.Current.CancellationToken));
        Assert.Null(await _store.GetAsync(Conversation, Guid.NewGuid(), Owner, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_Owner_Cannot_Read_The_File_Through_A_Different_Conversation() {
        var saved = await _store.SaveAsync(Conversation, Owner, "report.md", "text/markdown", Bytes("# report"), TestContext.Current.CancellationToken);

        Assert.Null(await _store.GetAsync(Guid.NewGuid(), saved.Id, Owner, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task An_Anonymous_Reader_Gets_Nothing(string? userId) {
        var saved = await _store.SaveAsync(Conversation, Owner, "report.md", "text/markdown", Bytes("x"), TestContext.Current.CancellationToken);

        Assert.Null(await _store.GetAsync(Conversation, saved.Id, userId!, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("report.html", "report.html")]
    [InlineData("../../etc/passwd", "passwd")]
    [InlineData(@"..\..\windows\system32\cmd.exe", "cmd.exe")]
    [InlineData("evil\"; filename=\"x.exe", "evil filename=x.exe")]
    [InlineData("line\r\nbreak.md", "linebreak.md")]
    [InlineData("a<b>c|d:e*f?.md", "abcdef.md")]
    [InlineData("   ", "file")]
    [InlineData("...", "file")]
    [InlineData("", "file")]
    public async Task File_Names_Are_Reduced_To_A_Plain_Name(string requested, string expected) {
        var saved = await _store.SaveAsync(Conversation, Owner, requested, "text/plain", Bytes("x"), TestContext.Current.CancellationToken);

        Assert.Equal(expected, saved.FileName);
    }

    [Fact]
    public async Task A_Very_Long_Name_Is_Shortened_But_Keeps_Its_Extension() {
        var saved = await _store.SaveAsync(Conversation, Owner, new string('a', 500) + ".html", "text/html", Bytes("x"), TestContext.Current.CancellationToken);

        Assert.True(saved.FileName.Length <= 200);
        Assert.EndsWith(".html", saved.FileName);
    }

    [Fact]
    public async Task Files_Above_The_Limit_Are_Refused() {
        var tooBig = new byte[ConversationFileStore.MaxFileBytes + 1];

        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(Conversation, Owner, "big.bin", "application/octet-stream", tooBig, TestContext.Current.CancellationToken));
        Assert.Empty(_db.Set<DbFile>());
    }

    [Fact]
    public async Task A_File_Needs_A_Conversation_An_Owner_And_A_Type() {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(Guid.Empty, Owner, "a.md", "text/markdown", Bytes("x"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(Conversation, " ", "a.md", "text/markdown", Bytes("x"), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentException>(() => _store.SaveAsync(Conversation, Owner, "a.md", "", Bytes("x"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task The_Same_Content_Saved_Twice_Gets_The_Same_Hash_And_Different_Ids() {
        var first = await _store.SaveAsync(Conversation, Owner, "a.md", "text/markdown", Bytes("same"), TestContext.Current.CancellationToken);
        var second = await _store.SaveAsync(Conversation, Owner, "a.md", "text/markdown", Bytes("same"), TestContext.Current.CancellationToken);

        Assert.Equal(first.Sha256, second.Sha256);
        Assert.NotEqual(first.Id, second.Id);
    }
}
