using System;

namespace Indice.Security;

/// <summary>Represent a tenant construct for multi-tenant applications.</summary>
public interface ITenant
{
    /// <summary>The tenant identifier.</summary>
    Guid Id { get; set; }
}

/// <summary>Represent a tenant construct for multi-tenant applications that has an alternate key named alias.</summary>
public interface ITenantWithAlias : ITenant
{
    /// <summary>The tenant identifier.</summary>
    string Alias { get; set; }
}
