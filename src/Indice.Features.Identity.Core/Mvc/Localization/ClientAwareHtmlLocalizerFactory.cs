using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace Indice.Features.Identity.Core.Mvc.Localization;

/// <summary>An extended <see cref="HtmlLocalizerFactory"/> that helps discover localized resources for client overridden views.</summary>
public class ClientAwareHtmlLocalizerFactory : HtmlLocalizerFactory
{
    /// <inheritdoc />
    public ClientAwareHtmlLocalizerFactory(IStringLocalizerFactory localizerFactory) : base(localizerFactory) { }

    /// <inheritdoc />
    public override IHtmlLocalizer Create(string baseName, string location) => base.Create(baseName.Replace("-", "_"), location);
}
