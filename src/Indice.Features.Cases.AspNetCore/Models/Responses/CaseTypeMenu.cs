using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Cases.Models.Responses;
/// <summary>The case type menu model.</summary>
public class CaseTypeMenu
{
    /// <summary>The Id of the case type.</summary>
    public Guid Id { get; set; }

    /// <summary>The case type title.</summary>
    public string Title { get; set; }

    /// <summary>
    /// The case type code
    /// </summary>
    public string Code { get; set; }

    /// <summary>Flag that promotes a case type to menu item.</summary>
    public bool IsMenuItem { get; set; }

    /// <summary>Data Grid filter options.</summary>
    public string? GridFilterConfig { get; set; }

    /// <summary>Data Grid column options.</summary>
    public string? GridColumnConfig { get; set; }
}
