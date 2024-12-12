﻿using Indice.Types;

namespace Indice.Features.Cases.Core.Models;

/// <summary>
/// Options used to filter the list of cases.
///
/// Will be used internally to filter cases further, based on authentication parameters
/// </summary>
public class GetCasesListFilter
{
    /// <summary>The Id of the customer to filter.</summary>
    public FilterClause[] CustomerIds { get; set; } = [];

    /// <summary>The name of the customer to filter.</summary>
    public FilterClause[] CustomerNames { get; set; } = [];

    /// <summary>The created date of the case, starting from, to filter.</summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>The creation date of the case, ending to, to filter.</summary>
    public DateTimeOffset? To { get; set; }

    /// <summary>The list of case type codes to filter.</summary>
    public FilterClause[] CaseTypeCodes { get; set; } = [];

    /// <summary>The list of checkpoint type Ids to filter.</summary>
    internal FilterClause[] CheckpointTypeIds { get; set; } = [];

    /// <summary>The list of checkpoint type codes to filter.</summary>
    public FilterClause[] CheckpointTypeCodes { get; set; } = [];

    /// <summary>The list of groupIds to filter.</summary>
    public FilterClause[] GroupIds { get; set; } = [];

    /// <summary>Construct filter clauses based on the metadata you are adding to the cases in your installation.</summary>
    public FilterClause[] Metadata { get; set; } = [];

    /// <summary>The reference number of the case to filter.</summary>
    public FilterClause[] ReferenceNumbers { get; set; } = [];

    /// <summary>Construct filter clauses based on case data.</summary>
    public FilterClause[] Data { get; set; } = [];
    /// <summary>Determines whether case data should be included in result.</summary>
    public bool? IncludeData { get; set; }
}
