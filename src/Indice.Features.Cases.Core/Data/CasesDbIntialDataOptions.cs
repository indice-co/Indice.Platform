using Indice.Features.Cases.Core.Data.Models;
namespace Indice.Features.Cases.Core.Data;
/// <summary>
/// Initial data for case seed
/// </summary>

public class CasesDbIntialDataOptions 
{

    /// <summary>The case types to seed</summary>
    public List<DbCaseType> CaseTypes { get; set; } = [];

    /// <summary>The cases to seed</summary>
    public List<DbCase> Cases { get; set; } = [];
}
