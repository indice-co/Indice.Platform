using Indice.Features.Cases.Core.Data.Models;

namespace Indice.Features.Cases.Core.Data;
/// <summary>
/// Initial data for case seed
/// </summary>
/// <param name="CaseTypes">The case types to seed</param>
/// <param name="Cases">The cases to seed</param>
public record CasesDbIntialDataOptions(List<DbCaseType> CaseTypes, List<DbCase> Cases);
