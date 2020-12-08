namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Extra query params to be used in the authorize request.
    /// </summary>
    public static class ExtraQueryParamNames
    {
        /// <summary>
        /// A direction to display a different screen when a client asks for the authorize endpoint.
        /// </summary>
        public const string Operation = "operation";
    }
}
