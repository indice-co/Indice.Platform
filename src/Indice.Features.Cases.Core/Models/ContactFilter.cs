﻿namespace Indice.Features.Cases.Core.Models;

/// <summary>The criteria to search for a customer to the consumer/integration service.</summary>
public class ContactFilter
{
    /// <summary>The Id of the customer as provided by the consumer/integrator.</summary>
    public string? Reference { get; set; }
    /// <summary>The case type code, used for filtering customers based on case type (implementantion on client code)</summary>
    public string? CaseTypeCode { get; set; }
}