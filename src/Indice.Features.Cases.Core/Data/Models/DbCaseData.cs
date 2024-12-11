﻿using Indice.Features.Cases.Core.Models;

namespace Indice.Features.Cases.Core.Data.Models;

#pragma warning disable 1591
public class DbCaseData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CaseId { get; set; }
    public AuditMeta CreatedBy { get; set; } = null!;
    public dynamic? Data { get; set; } = null!;
    public virtual DbCase Case { get; set; } = null!;
}
#pragma warning restore 1591
