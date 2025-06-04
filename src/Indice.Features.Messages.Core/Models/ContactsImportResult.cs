namespace Indice.Features.Messages.Core.Models;

/// <summary>
/// Encapsulates the result after importing contacts into a distribution list.
/// </summary>
public sealed class ContactsImportResult
{
    /// <summary>
    /// The number of the contacts successfully created.
    /// </summary>
    public int ContactsAdded { get; set; } = 0;

    /// <summary>
    /// The number of the existing contacts successfully updated.
    /// </summary>
    public int ContactsUpdated { get; set; } = 0;

    /// <summary>
    /// The errors occured duing the import process.
    /// </summary>
    public List<string> Errors { get; set; } = [];
}
