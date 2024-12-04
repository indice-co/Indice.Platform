using System.Net.Mime;
using Indice.AspNetCore.Mvc.ApplicationModels;
using Indice.Features.Messages.AspNetCore.Mvc.Formatters;
using Indice.Features.Messages.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.AspNetCore.Mvc;
internal class MessageManagementConfigureMvcOptions(IOptions<MessageManagementOptions> apiOptions) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options) {
        options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.CampaignManagementEndpoints, apiOptions.Value.PathPrefix));
        options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.CampaignManagementEndpoints, apiOptions.Value.GroupName));
        options.FormatterMappings.SetMediaTypeMappingForFormat("xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeNames.Application.Json);
        options.OutputFormatters.Add(new XlsxCampaignStatisticsOutputFormatter());
    }
}
internal class MessageInboxConfigureMvcOptions(IOptions<MessageInboxOptions> apiOptions) : IConfigureOptions<MvcOptions>
{
    public void Configure(MvcOptions options) {
        options.Conventions.Add(new ApiPrefixControllerModelConvention(ApiPrefixes.MessageInboxEndpoints, apiOptions.Value.PathPrefix));
        options.Conventions.Add(new ApiGroupNameControllerModelConvention(ApiGroups.MessageInboxEndpoints, apiOptions.Value.GroupName));
        options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeNames.Application.Json);
    }
}
