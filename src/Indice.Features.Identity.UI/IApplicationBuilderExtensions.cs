using System.Reflection;
using Indice.Features.Identity.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/> interface.</summary>
public static class IApplicationBuilderExtensions
{
    /// <summary>Coordinates the page selection process for pages that need to be overridden for specified clients.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <remarks>Should be used after UseRouting() method is called.</remarks>
    [Obsolete("This is not needed and should be removed. Actually the method contents have commented out and it does nothing. Now the theme selection happens internally through conventions")]
    public static void UseIdentityUIThemes(this IApplicationBuilder app) { 
    
    }
}
