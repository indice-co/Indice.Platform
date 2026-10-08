using Indice.Types;
using Xunit;

namespace Indice.Common.Tests;

public class ConnectionStringTests
{
    [Fact]
    public void Can_Parse_Connection_String() {
        var connectionString = new ConnectionString("Server=(localdb)\\MSSQLLocalDB;Database=Indice.Id;Trusted_Connection=True;MultipleActiveResultSets=true");
        Assert.Equal("(localdb)\\MSSQLLocalDB", connectionString["Server"]);
        Assert.Equal("Indice.Id", connectionString["Database"]);
        Assert.Equal("True", connectionString["Trusted_Connection"]);
        Assert.Equal("true", connectionString["MultipleActiveResultSets"]);
        Assert.Throws<KeyNotFoundException>(() => connectionString["AnUnknownKey"]);
        Assert.False(connectionString.ContainsKey("AnUnknownKey"));
    }

    [Fact]
    public void Can_Parse_Value_Containing_Equals_Sign() {
        // SAS tokens and Base64 strings commonly embed '=' inside a value.
        const string sasToken = "sv=2021-06-08;sig=abc+def/ghi==;se=2024-01-01T00:00:00Z";
        var connectionString = new ConnectionString(sasToken);
        Assert.Equal("2021-06-08", connectionString["sv"]);
        Assert.Equal("abc+def/ghi==", connectionString["sig"]);
        Assert.Equal("2024-01-01T00:00:00Z", connectionString["se"]);
    }

    [Fact]
    public void ToString_Roundtrips_Connection_String() {
        const string raw = "Server=(localdb)\\MSSQLLocalDB;Database=Indice.Id;Trusted_Connection=True";
        var connectionString = new ConnectionString(raw);
        Assert.Equal(raw, connectionString.ToString());
    }

    [Fact]
    public void ToString_Preserves_Values_Containing_Equals_Sign() {
        const string raw = "AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        var connectionString = new ConnectionString(raw);
        Assert.Equal(raw, connectionString.ToString());
    }

    [Fact]
    public void Copy_Constructor_Creates_Independent_Copy() {
        var original = new ConnectionString("Server=localhost;Database=MyDb");
        var copy = new ConnectionString(original);
        copy.Remove("Database");
        Assert.True(original.ContainsKey("Database"));
        Assert.False(copy.ContainsKey("Database"));
        Assert.Equal(original.Delimiter, copy.Delimiter);
    }

    [Fact]
    public void Remove_Eliminates_Key_From_Connection_String() {
        var connectionString = new ConnectionString("Server=localhost;Database=MyDb;Trusted_Connection=True");
        connectionString.Remove("Database");
        Assert.False(connectionString.ContainsKey("Database"));
        Assert.True(connectionString.ContainsKey("Server"));
        Assert.True(connectionString.ContainsKey("Trusted_Connection"));
    }

    [Fact]
    public void Remove_Then_ToString_Omits_Removed_Key() {
        var connectionString = new ConnectionString("Server=localhost;Database=MyDb;Trusted_Connection=True");
        connectionString.Remove("Database");
        Assert.Equal("Server=localhost;Trusted_Connection=True", connectionString.ToString());
    }
}
