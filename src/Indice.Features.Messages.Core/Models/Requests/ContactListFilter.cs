namespace Indice.Features.Messages.Core.Models.Requests;

/// <summary>Options used to filter the list of templates.</summary>
public class TemplateListFilter
{
    /// <summary>The id of a Message type.</summary>
    public Guid? MessageTypeId { get; set; }

    /// <summary>Fetch items without Message type id.</summary>
    public bool? IncludeItemsWithoutMessageTypeId { get; set; }
}
