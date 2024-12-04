using Indice.Features.Messages.Core.Models;

namespace Indice.Features.Messages.Core.Data.Models;

/// <summary>Template entity.</summary>
public class DbTemplate : DbAuditableEntity
{
    /// <summary>The unique id of the template.</summary>
    public Guid Id { get; set; }
    /// <summary>The name of the template.</summary>
    public string Name { get; set; }
    /// <summary>The contents of the template.</summary>
    public MessageContentDictionary Content { get; set; } = new();
    /// <summary>Sample data for the testing the template preview.</summary>
    /// <remarks>Optional</remarks>
    public dynamic Data { get; set; }
}
