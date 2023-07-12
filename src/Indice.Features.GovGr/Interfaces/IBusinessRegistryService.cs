using Indice.Features.GovGr.Models;

namespace Indice.Features.GovGr.Interfaces;

/// <summary>GovGr Business Registry service definition</summary>
public interface IBusinessRegistryService
{
    /// <summary>
    /// Gets Business Registry from AADE
    /// </summary>
    /// <param name="fiscalCode"></param>
    /// <returns></returns>
    Task<BusinessRegistryRecord> GetBusinessRegistry(string fiscalCode);
}
