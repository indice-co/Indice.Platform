using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Indice.Features.Identity.UI;

internal class OrderPageRouteModelConvention : IPageRouteModelConvention
{
    private readonly int _order = 0;

    public OrderPageRouteModelConvention(int order) => _order = order;

    public void Apply(PageRouteModel model) {
        var libraryPagesRegex = new Regex(@"\/Pages\/[A-Za-z]*.cshtml");
        if (!libraryPagesRegex.IsMatch(model.RelativePath)) {
            return;
        }
        var selector = model.Selectors.FirstOrDefault();
        if (selector?.AttributeRouteModel is not null) {
            selector.AttributeRouteModel.Order = _order;
        }
    }
}
