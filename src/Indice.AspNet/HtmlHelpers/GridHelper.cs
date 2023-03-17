using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using Indice.AspNet.Resources;
using Indice.AspNet.Types;

namespace Indice.AspNet.HtmlHelpers;

public static class GridHelper
{
    static Regex camelCaseRegex = new Regex(@"\B\p{Lu}\p{Ll}", RegexOptions.Compiled);

    static string DisplayNameFromCamelCase(string name) {
        name = camelCaseRegex.Replace(name, " $0");

        if (name.EndsWith(" Id")) {
            name = name.Substring(0, name.Length - 3);
        }

        return name;
    }

    #region Grid
    public class GridColumnBase
    {
        private string _name = null;
        private string _sortExpression = null;

        public string Name {
            get {
                if (string.IsNullOrWhiteSpace(_name)) {
                    return DisplayNameFromCamelCase(Meta.GetDisplayName());
                }

                return _name;
            }
            set { _name = value; }
        }

        public string Expression { get; private set; }

        public string SortExpression {
            get { return _sortExpression ?? Expression; }
            set { _sortExpression = value; }
        }

        public ModelMetadata Meta { get; private set; }
        public string HeadClass { get; set; }
        public string Format { get; set; }
        public string Class { get; set; }
        public string IconClass { get; set; }
        public bool IsSortable { get; set; }
        public bool IsIcon { get; set; }
        public bool IsValueCssClass { get; set; }
        public bool IsRowCssClass { get; set; }
        public bool IsHidden { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public Func<object, HelperResult> Template { get; set; }
        public bool HasHeader { get; set; }
        public bool HasLink => !string.IsNullOrWhiteSpace(Action);

        public GridColumnBase(string expression, ModelMetadata meta, string @class = "text-left", string headClass = "text-left") {
            Expression = expression;
            Meta = meta;
            Class = @class;
            IsSortable = true;
            HasHeader = true;
            Format = "{0}";
        }
    }

    public class GridColumn<TModel, TValue> : GridColumnBase
    {
        public GridColumn(Expression<Func<TModel, TValue>> expression, string @class = "text-left", string headClass = "text-left")
            : base(ExpressionHelper.GetExpressionText(expression), ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<TModel>()), @class) {
        }
    }

    public static GridColumn<TModel, TValue> NewGridColumn<TModel, TValue>(this IEnumerable<TModel> list, Expression<Func<TModel, TValue>> expression, string @class = "text-left", string headClass = "text-left", bool sortable = true) {
        return new GridColumn<TModel, TValue>(expression, @class) {
            IsSortable = sortable
        };
    }

    public static GridColumnBase NewGridColumn(this IPaginatedList list, string expression, string @class = "text-left", string headClass = "text-left", bool sortable = true) {
        var collectionType = list.GetType();
        var itemType = collectionType.GetElementType();

        if (null == itemType) {
            var interfaces = collectionType.GetInterfaces();

            foreach (var i in interfaces) {
                if (i.IsGenericType && i.GetGenericTypeDefinition().Equals(typeof(IEnumerable<>))) {
                    itemType = i.GetGenericArguments()[0];
                }
            }
        }

        object instance = null;
        ModelMetadata meta = null;

        if (IsAnonymousType(itemType)) {
            var initValues = Enumerable.Range(1, itemType.GetProperties().Length).Select<int, object>(i => null).ToArray();
            instance = Activator.CreateInstance(itemType, initValues);
            meta = ModelMetadata.FromStringExpression(expression, new ViewDataDictionary(instance));
        } else if (itemType.IsAbstract) {
            meta = ModelMetadataProviders.Current.GetMetadataForType(null, itemType);
        } else {
            instance = Activator.CreateInstance(itemType);
            meta = ModelMetadata.FromStringExpression(expression, new ViewDataDictionary(instance));
        }

        return new GridColumnBase(expression, meta, @class, headClass) {
            IsSortable = sortable
        };
    }

    private static bool IsAnonymousType(Type type) {
        var hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Count() > 0;
        var nameContainsAnonymousType = type.FullName.Contains("AnonymousType");
        var isAnonymousType = hasCompilerGeneratedAttribute && nameContainsAnonymousType;

        return isAnonymousType;
    }

    public static T NavigatesTo<T>(this T column, string action, string controller) where T : GridColumnBase {
        column.Action = action;
        column.Controller = controller;

        return column;
    }

    public static T AsIcon<T>(this T column, string iconClass) where T : GridColumnBase {
        column.IconClass = iconClass;
        column.IsIcon = true;

        return column;
    }

    public static T UseValueAsCssClass<T>(this T column) where T : GridColumnBase {
        column.IsValueCssClass = true;
        return column;
    }

    public static T RowCssClass<T>(this T column) where T : GridColumnBase {
        column.IsRowCssClass = true;
        return column;
    }

    public static T Hidden<T>(this T column, bool isHidden = true) where T : GridColumnBase {
        column.IsHidden = isHidden;
        return column;
    }

    public static GridColumn<TModel, TValue> Sortable<TModel, TValue>(this GridColumn<TModel, TValue> column, bool sortable = true, Expression<Func<TModel, TValue>> sortexpression = null) {
        column.IsSortable = sortable;
        column.SortExpression = sortexpression != null ? ExpressionHelper.GetExpressionText(sortexpression) : null;

        return column;
    }

    public static T Sortable<T>(this T column, bool sortable, string sortexpression) where T : GridColumnBase {
        column.IsSortable = sortable;
        column.SortExpression = sortexpression;

        return column;
    }

    public static T NoHeader<T>(this T column) where T : GridColumnBase {
        column.HasHeader = false;
        return column;
    }

    public static T WithName<T>(this T column, string name) where T : GridColumnBase {
        column.Name = name;
        return column;
    }

    public static T DisplayFormat<T>(this T column, string format) where T : GridColumnBase {
        column.Format = format;
        return column;
    }

    public static T WithTemplate<T>(this T column, Func<dynamic, HelperResult> template) where T : GridColumnBase {
        column.Template = template;
        return column;
    }

    private static readonly string CELL_TEXT_HTML = "<td class=\"{1}\"><span title=\"{0}\">{0}</span></td>";
    private static readonly string CELL_TEMPLATE_HTML = "<td class=\"{1}\">{0}</td>";
    private static readonly string CELL_LINK_HTML = "<td class=\"{1}\"><a title=\"{0}\" href=\"{2}\" data-key-value=\"{3}\">{0}</a></td>";
    private static readonly string CELL_TEXT_ICON_HTML = "<td class=\"{1}\"><i title=\"{0}\" data-key-value=\"{2}\" class=\"{3}\"></i></td>";
    private static readonly string CELL_LINK_ICON_HTML = "<td class=\"{1}\"><a title=\"{0}\" href=\"{2}\" data-key-value=\"{3}\"><i class=\"{4}\"></i></a></td>";
    private const int MINIMUM_WINDOW = 7;
    private const int MINUMUM_PAGES = MINIMUM_WINDOW + 3;

    public static MvcHtmlString GridFor<TItem, TKey>(this HtmlHelper<IEnumerable<TItem>> html, Expression<Func<IEnumerable<TItem>, IPaginatedList>> expression, Expression<Func<TItem, TKey>> keyExpression, object htmlAttributes, params GridColumnBase[] columns) =>
        GridFor(html, expression, ExpressionHelper.GetExpressionText(keyExpression), HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), columns);

    public static MvcHtmlString GridFor<TModel>(this HtmlHelper<TModel> html, Expression<Func<TModel, IPaginatedList>> expression, string keyExpression, object htmlAttributes, params GridColumnBase[] columns) =>
        GridFor(html, expression, keyExpression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), columns);

    public static MvcHtmlString GridFor<TModel>(this HtmlHelper<TModel> html, Expression<Func<TModel, IPaginatedList>> expression, string keyExpression, IDictionary<string, object> htmlAttributes, params GridColumnBase[] columns) {
        var expressionText = ExpressionHelper.GetExpressionText(expression);

        var list = (IPaginatedList)(ModelMetadata.FromStringExpression(expressionText, html.ViewData).Model ??
                                    ModelMetadata.FromLambdaExpression(expression, html.ViewData).Model);

        var urlHelper = new UrlHelper(html.ViewContext.RequestContext);
        var currentRouteData = urlHelper.CurrentRouteData();
        var table = new TagBuilder("table");
        var headerBuilder = new StringBuilder();
        headerBuilder.AppendLine("<tr>");

        foreach (var column in columns) {
            if (column.IsHidden) {
                continue;
            }

            var headerText = column.HasHeader ? column.Name : string.Empty;
            var cell = string.Format("<th class=\"{1}\"><div>{0}</div></th>", HttpUtility.HtmlEncode(headerText), column.HeadClass);

            if (column.IsSortable && column.HasHeader) {
                var sortExpression = ExpressionHelper.GetExpressionText(column.SortExpression);
                var tooltip = "Sort Ascending";
                var headClass = "not-sorted";
                // Sort ascending by default.
                var sortUrl = urlHelper.Current(new { sort = sortExpression, sortDir = string.Empty }).ToString();
                var isSorted = currentRouteData.ContainsKey("sort") && currentRouteData["sort"].ToString().ToLower() == sortExpression.ToLower();

                if (isSorted) {
                    var isDesc = currentRouteData.ContainsKey("sortdir") && currentRouteData["sortdir"].ToString().ToLower() == "desc";

                    if (!isDesc) {
                        tooltip = " Sort descending";
                        headClass = "sort-asc";
                        sortUrl = urlHelper.Current(new { sort = sortExpression, sortDir = "desc" }).ToString();
                    } else {
                        tooltip = "No sort";
                        headClass = "sort-desc";
                        sortUrl = urlHelper.Current(new { sort = string.Empty, sortDir = string.Empty }).ToString();
                    }
                }

                column.HeadClass += " " + headClass;
                cell = string.Format("<th class=\"sortable {1}\"><div><a title=\"{2}\" href=\"{3}\">{0}</a><i class=\"grip\"></i></div></th>", HttpUtility.HtmlEncode(headerText), column.HeadClass, tooltip, sortUrl);
            }

            headerBuilder.AppendLine(cell);
        }

        headerBuilder.AppendLine("</tr>");

        var thead = new TagBuilder("thead") {
            InnerHtml = headerBuilder.ToString()
        };

        var bodyBuilder = new StringBuilder();

        foreach (var item in list) {
            var viewData = new ViewDataDictionary(item);
            var keyExpressionText = ExpressionHelper.GetExpressionText(keyExpression);
            var keyMeta = ModelMetadata.FromStringExpression(keyExpressionText, viewData);

            var routeValues = new RouteValueDictionary() {
                { keyMeta.PropertyName, keyMeta.Model }
            };

            var rowClassNames = string.Join(" ", columns.Where(c => c.IsRowCssClass).Select(c => ExpressionHelper.GetExpressionText(c.Expression) + "-" + ModelMetadata.FromStringExpression(ExpressionHelper.GetExpressionText(c.Expression), viewData).Model.ToString()).ToArray());
            rowClassNames = rowClassNames.ToLower().Replace(".", "-");
            var rowTag = string.IsNullOrWhiteSpace(rowClassNames) ? "<tr>" : string.Format("<tr class=\"{0}\" data-{1}=\"{2}\">", rowClassNames, keyMeta.PropertyName.ToLower(), keyMeta.Model);
            bodyBuilder.AppendLine(rowTag);

            foreach (var column in columns) {
                if (column.IsHidden) {
                    continue;
                }

                object value = null;
                var itemexpressionText = ExpressionHelper.GetExpressionText(column.Expression);
                var propMeta = ModelMetadata.FromStringExpression(itemexpressionText, viewData);
                var format = propMeta.DisplayFormatString ?? column.Format;
                value = string.Format(format, propMeta.Model, CultureInfo.CurrentUICulture) ?? propMeta.NullDisplayText;

                if (column.IsValueCssClass) {
                    column.Class += " " + value;
                }

                if (column.HasLink && column.Template == null) {
                    var linkText = string.IsNullOrWhiteSpace(value.ToString()) ? "(empty)" : value.ToString();
                    var url = urlHelper.Action(column.Action, column.Controller, routeValues);
                    bodyBuilder.AppendLine(string.Format(column.IsIcon ? CELL_LINK_ICON_HTML : CELL_LINK_HTML, HttpUtility.HtmlEncode(linkText), column.Class, url, HttpUtility.HtmlEncode(keyMeta.Model), column.IconClass));
                } else {
                    if (column.Template != null) {
                        bodyBuilder.AppendLine(string.Format(CELL_TEMPLATE_HTML, column.Template(viewData.Model).ToHtmlString(), column.Class, HttpUtility.HtmlEncode(keyMeta.Model), column.IconClass));
                    } else {
                        bodyBuilder.AppendLine(string.Format(column.IsIcon ? CELL_TEXT_ICON_HTML : CELL_TEXT_HTML, HttpUtility.HtmlEncode(value), column.Class, HttpUtility.HtmlEncode(keyMeta.Model), column.IconClass));
                    }
                }
            }

            bodyBuilder.AppendLine("</tr>");
        }

        if (list.TotalCount == 0) {
            bodyBuilder.AppendLine(string.Format("<tr><td colspan=\"{0}\" class=\"no-records\">{1}</td></tr>", columns.Length, "No records found."));
        }

        var tbody = new TagBuilder("tbody") {
            InnerHtml = bodyBuilder.ToString()
        };

        var content = new StringBuilder();
        content.AppendLine(thead.ToString(TagRenderMode.Normal));
        content.AppendLine(tbody.ToString(TagRenderMode.Normal));
        table.InnerHtml = content.ToString();
        table.MergeAttributes(htmlAttributes, replaceExisting: true);
        table.AddCssClass("grid");

        return new MvcHtmlString("<div class=\"data-grid table-responsive\">" + table.ToString(TagRenderMode.Normal) + "</div>");
    }
    #endregion

    #region Pager For
    public static MvcHtmlString PagerFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, int window = MINIMUM_WINDOW, object htmlAttributes = null) where TValue : IPaginatedList =>
        PagerFor(html, expression, window, new RouteValueDictionary(htmlAttributes));

    public static MvcHtmlString PagerFor<TModel, TValue>(this HtmlHelper<TModel> html, Expression<Func<TModel, TValue>> expression, int window, IDictionary<string, object> htmlAttributes) where TValue : IPaginatedList {
        if (expression == null) {
            throw new ArgumentNullException(nameof(expression));
        }

        var expressionText = ExpressionHelper.GetExpressionText(expression);
        var list = (IPaginatedList)(ModelMetadata.FromStringExpression(expressionText, html.ViewData).Model ?? ModelMetadata.FromLambdaExpression(expression, html.ViewData).Model);
        window = AdjustWindow(window, list.TotalPages);
        var urlHelper = new UrlHelper(html.ViewContext.RequestContext);
        var container = new TagBuilder("ul");
        container.MergeAttributes(htmlAttributes);
        container.MergeAttribute("class", "pagination");
        var innerHtml = new StringBuilder();
        innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Previous, window, null).ToString());
        innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, 1).ToString());
        var windowFactor = ((window - 2) / 2);
        IEnumerable<int> pages;
        var nearStart = list.Page - windowFactor <= 3;
        var nearEnd = list.TotalPages - list.Page - windowFactor < 3;

        if (list.TotalPages < MINUMUM_PAGES) {
            pages = Enumerable.Range(2, list.TotalPages - 2);

            foreach (var page in pages) {
                innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, page).ToString());
            }
        } else {
            if (nearStart) {
                pages = Enumerable.Range(2, window - 1);

                foreach (var page in pages) {
                    innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, page).ToString());
                }

                if (pages.Any()) {
                    innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Space, window, list.Page).ToString());
                }
            }

            if (nearEnd) {
                pages = Enumerable.Range(list.TotalPages - window + 1, window - 1);

                if (pages.Any()) {
                    innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Space, window, list.Page).ToString());
                }

                foreach (var page in pages) {
                    innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, page).ToString());
                }
            }

            if (!nearStart && !nearEnd) {
                pages = Enumerable.Range(list.Page - windowFactor, window - 2);
                innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Space, window, list.Page).ToString());

                foreach (var page in pages) {
                    innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, page).ToString());
                }

                innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Space, window, list.Page).ToString());
            }
        }

        innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Numbers, window, list.TotalPages).ToString());
        innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Next, window, null).ToString());
        innerHtml.AppendLine(GetPagerLink(html, urlHelper, list, PagerMoveType.Summary, window, null).ToString());
        container.InnerHtml = innerHtml.ToString();

        return MvcHtmlString.Create(container.ToString());
    }

    private static TagBuilder GetPagerLink(HtmlHelper html, UrlHelper url, IPaginatedList list, int window, int? page) => 
        GetPagerLink(html, url, list, PagerMoveType.Numbers, window, page);

    private static TagBuilder GetPagerLink(HtmlHelper html, UrlHelper url, IPaginatedList list, PagerMoveType type, int window, int? page) {
        var container = new TagBuilder("li");
        var inner = new TagBuilder("a");
        var @class = string.Empty;
        var href = string.Empty;
        var text = string.Empty;
        var actionUrl = string.Empty;

        switch (type) {
            case PagerMoveType.Previous:
                actionUrl = url.Current(new { page = list.Page - 1 }).ToString();
                inner = new TagBuilder("a");
                text = "‹";
                href = list.HasPreviousPage ? actionUrl : string.Empty;
                @class = list.HasPreviousPage ? "prev" : "prev disabled";

                if (list.HasPreviousPage) {
                    inner.MergeAttribute("href", href);
                }

                inner.MergeAttribute("title", PagerStrings.PagerPrevious);
                break;
            case PagerMoveType.Next:
                actionUrl = url.Current(new { page = (list.Page + 1) }).ToString();
                inner = new TagBuilder("a");
                text = "›";
                href = list.HasNextPage ? actionUrl : string.Empty;
                @class = list.HasNextPage ? "next" : "next disabled";

                if (list.HasNextPage) {
                    inner.MergeAttribute("href", href);
                }

                inner.MergeAttribute("title", PagerStrings.PagerNext);
                break;
            case PagerMoveType.Numbers:
                actionUrl = url.Current(new { page }).ToString();
                inner = new TagBuilder("a");
                text = string.Format("{0}", page);
                href = list.Page != page ? actionUrl : string.Empty;
                @class = list.Page != page ? "" : "active";

                if (list.Page != page) {
                    inner.MergeAttribute("href", href);
                }

                break;
            case PagerMoveType.Space:
                inner = new TagBuilder("a");
                text = "...";
                @class = "space";
                break;
            case PagerMoveType.Summary:
                inner = new TagBuilder("span");
                text = string.Format(PagerStrings.PagerSummary, list.Page, list.TotalPages, list.TotalCount);
                @class = "pager-summary";
                break;
            default:
                break;
        }

        inner.InnerHtml = text;
        container.MergeAttribute("class", @class);
        container.InnerHtml = inner.ToString();

        return container;
    }

    public enum PagerMoveType
    {
        Previous,
        Next,
        Summary,
        Numbers,
        Space
    }

    private static int AdjustWindow(int window, int totalPages) {
        if (window < MINIMUM_WINDOW || window > totalPages - 3) {
            return MINIMUM_WINDOW;
        }

        if (window % 2 == 0) {
            window = window - 1;
        }

        return window;
    }
    #endregion

    #region Internal helpers
    internal static RouteValueDictionary CurrentRouteData(this UrlHelper helper) {
        // Get the route data for the current URL e.g. /Research/InvestmentModelling/RiskComparison.
        // This is needed because unlike UrlHelper.Action, UrlHelper.RouteUrl sets includeImplicitMvcValues to false which causes it to ignore current 
        // ViewContext.RouteData.Values.
        var routeValues = new RouteValueDictionary(helper.RequestContext.RouteData.Values);
        // Get the current query string e.g. ?BucketID=17371&compareTo=123.
        var queryString = helper.RequestContext.HttpContext.Request.QueryString;

        // Add query string parameters to the route value dictionary.
        foreach (string param in queryString) {
            if (!string.IsNullOrEmpty(queryString[param])) {
                routeValues[param] = queryString[param];
            }
        }

        return routeValues;
    }

    internal static string Current(this UrlHelper helper, object substitutes) {
        // Get the route data for the current URL e.g. /Research/InvestmentModelling/RiskComparison.
        // This is needed because unlike UrlHelper.Action, UrlHelper.RouteUrl sets includeImplicitMvcValues to false
        // which causes it to ignore current ViewContext.RouteData.Values.
        var routeValues = new RouteValueDictionary(helper.RequestContext.RouteData.Values);
        // Get the current query string e.g. ?BucketID=17371&compareTo=123.
        var queryString = helper.RequestContext.HttpContext.Request.QueryString;

        //add query string parameters to the route value dictionary
        foreach (string param in queryString) {
            if (!string.IsNullOrEmpty(queryString[param])) {
                routeValues[param] = queryString[param];
            }
        }

        //override parameters we're changing
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(substitutes.GetType())) {
            var value = property.GetValue(substitutes);

            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) {
                routeValues.Remove(property.Name);
            } else {
                routeValues[property.Name] = value;
            }
        }

        // UrlHelper will find the first matching route (the routes are searched in the order they were registered).
        // The unmatched parameters will be added as query string.
        var url = helper.RouteUrl(routeValues);

        return url;
    }

    internal static string Current(this UrlHelper helper, object substitutes, ref RouteValueDictionary resultRouteData) {
        // Get the route data for the current URL e.g. /Research/InvestmentModelling/RiskComparison.
        // This is needed because unlike UrlHelper.Action, UrlHelper.RouteUrl sets includeImplicitMvcValues to false
        // which causes it to ignore current ViewContext.RouteData.Values
        resultRouteData = new RouteValueDictionary(helper.RequestContext.RouteData.Values);
        // Get the current query string e.g. ?BucketID=17371&compareTo=123
        var queryString = helper.RequestContext.HttpContext.Request.QueryString;

        //add query string parameters to the route value dictionary
        foreach (string param in queryString) {
            if (!string.IsNullOrEmpty(queryString[param])) {
                resultRouteData[param] = queryString[param];
            }
        }

        //override parameters we're changing
        foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(substitutes.GetType())) {
            var value = property.GetValue(substitutes);

            if (value == null || string.IsNullOrWhiteSpace(value.ToString())) {
                resultRouteData.Remove(property.Name);
            } else {
                resultRouteData[property.Name] = value;
            }
        }

        // UrlHelper will find the first matching route (the routes are searched in the order they were registered).
        // The unmatched parameters will be added as query string.
        var url = helper.RouteUrl(resultRouteData);

        return url;
    }
    #endregion
}
