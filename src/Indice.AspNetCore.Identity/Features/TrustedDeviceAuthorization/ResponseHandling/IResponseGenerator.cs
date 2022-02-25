using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.ResponseHandling
{
    internal interface IResponseGenerator<TResult, TResponse> 
        where TResult : class
        where TResponse : class
    {
        Task<TResponse> Generate(TResult validationResult);
    }
}
