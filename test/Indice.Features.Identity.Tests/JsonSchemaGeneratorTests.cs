#nullable enable
using System.Text.Json;
using Indice.Features.Identity.Core.Extensions;
using Json.Schema.Generation;
using Xunit;

namespace Indice.Features.Identity.Tests;
public class JsonSchemaGeneratorTests
{
    [Fact]
    public void TestJsonSchemaGeneratorFromClrType() {
        var schema = typeof(TestClassWithConventions).ToJsonSchema();
        var json = schema.AsJsonString();
    }

    [Json.Schema.Generation.Nullable(false)]
    public class TestClassWithConventions
    {
        [Json.Schema.Generation.Nullable(false)]
        [Required]
        public string DisplayName { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Age { get; set; } = 18;
        public DateTimeOffset? DateTimeCreated { get; set; } = DateTimeOffset.Now;
        public bool Optional { get; set; }

        public GenderType? Gender { get; set; }
    }

    public enum GenderType
    { 
        Other = 0,
        Male = 1,
        Female = 2
    }
}
#nullable disable