using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    internal interface IResponseGenerator<TResult, TResponse> 
        where TResult : class
        where TResponse : class
    {
        Task<TResponse> Generate(TResult validationResult);
    }
}
