namespace Indice.Features.Identity.Core.DeviceAuthentication.ResponseHandling;

internal interface IResponseGenerator<TResult, TResponse> 
    where TResult : class
    where TResponse : class
{
    Task<TResponse> Generate(TResult validationResult);
}
