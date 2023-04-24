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
}
