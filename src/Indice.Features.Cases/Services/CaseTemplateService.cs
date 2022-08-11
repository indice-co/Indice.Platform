using System;
using System.IO;
using System.Threading.Tasks;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Indice.Features.Cases.Services
{
    public class CaseTemplateService : ICaseTemplateService
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITempDataProvider _tempDataProvider;

        public CaseTemplateService(
            IRazorViewEngine viewEngine,
            IHttpContextAccessor httpContextAccessor,
            ITempDataProvider tempDataProvider
            ) {
            _viewEngine = viewEngine ?? throw new ArgumentNullException(nameof(viewEngine));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _tempDataProvider = tempDataProvider ?? throw new ArgumentNullException(nameof(tempDataProvider));
        }

        public async Task<string> RenderTemplateAsync(CaseDetails @case) {
            return await RenderTemplateAsync($"Cases/Pdf/{@case.CaseType.Code}", @case);
        }

        public async Task<string> RenderTemplateAsync<T>(string viewName, T viewModel) {
            var httpContext = _httpContextAccessor.HttpContext;
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var outputWriter = new StringWriter()) {
                var viewResult = _viewEngine.FindView(actionContext, viewName, false);
                if (!viewResult.Success) {
                    throw new RenderTemplateServiceException($"Failed to render template {viewName} because it was not found.");
                }

                var viewDictionary = new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
                    Model = viewModel
                };

                var tempDataDictionary = new TempDataDictionary(httpContext, _tempDataProvider);

                try {
                    var viewContext = new ViewContext(
                        actionContext,
                        viewResult.View,
                        viewDictionary,
                        tempDataDictionary,
                        outputWriter,
                        new HtmlHelperOptions());

                    await viewResult.View.RenderAsync(viewContext);
                } catch (Exception ex) {
                    throw new RenderTemplateServiceException("Failed to render template due to a razor engine failure", ex);
                }

                return outputWriter.ToString();
            };
        }
    }
}
