namespace Indice.Features.Cases.Core;

/// <summary>Options and settings</summary>
public class CasesOptions
{
    /// <summary>The default scope name to be used for Cases API. Defaults to <strong>cases</strong>.</summary>
    public string RequiredScope { get; set; } = CasesCoreConstants.DefaultScopeName;

    /// <summary>Enables the Case `ReferenceNumber` feature. Defaults to <see langword="false"/>.</summary>
    public bool ReferenceNumberEnabled { get; set; }
    /// <summary>The claim type groupid name</summary>
    public string GroupIdClaimType { get; set; } = CasesCoreConstants.DefaultGroupIdClaimType;
}
