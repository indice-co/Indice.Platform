using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;
/// <summary>
/// Drives the menu service for case types
/// </summary>
public interface ICaseTypeMenuService
{
    /// <summary>
    /// Get case types in a menu format
    /// </summary>
    /// <returns>Result set of <see cref="CaseTypeMenu"/></returns>
    Task<ResultSet<CaseTypeMenu>> GetMenuItems(ListOptions options);
}
