using Indice.Features.Cases.Core.Data.Models;

namespace Indice.Features.Cases.Core.Data;
/// <summary>
/// Initial data for case seed
/// </summary>
/// <param name="CaseTypes"></param>
public record CasesDbIntialDataOptions(List<DbCaseType> CaseTypes);
