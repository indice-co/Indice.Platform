using Indice.Features.Messages.AspNetCore.Endpoints;
using Indice.Features.Messages.Core;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Security;
using Indice.Services;
using Indice.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OfficeOpenXml.FormulaParsing.Excel.Functions.DateTime;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides endpoints for managing campaign-related operations, including retrieving, creating, updating, publishing, 
/// and deleting campaigns, as well as handling attachments and statistics.
/// </summary>
internal static class CampaignsApi
{
    /// <summary>Registers the endpoints for Campaigns API.</summary>
    /// <param name="routes">Defines a contract for a route builder in an application. A route builder specifies the routes for an application.</param>
    public static RouteGroupBuilder MapCampaigns(this IEndpointRouteBuilder routes) {
        var options = routes.ServiceProvider.GetRequiredService<IOptions<MessageManagementOptions>>().Value;
        var group = routes.MapGroup(options.PathPrefix.TrimEnd('/') + "/campaigns");
        if (!string.IsNullOrEmpty(options.GroupName)) {
            group.WithGroupName(options.GroupName);
        }
        group.WithTags("Campaigns");
        var allowedScopes = new[] { options.RequiredScope }.Where(x => x != null).ToArray();

        group.RequireAuthorization(pb => pb.AddAuthenticationSchemes(MessagesApi.AuthenticationScheme)
                                           .RequireAuthenticatedUser()

                                           .RequireCampaignsManagement()
                                           .RequireClaim(BasicClaimTypes.Scope, allowedScopes));

        group.AddOpenApiSecurityRequirement("oauth2", allowedScopes).WithOpenApiSecurityRequirement("oauth2", allowedScopes);

        group.WithHandledException<BusinessException>()
             .ProducesProblem(StatusCodes.Status401Unauthorized)
             .ProducesProblem(StatusCodes.Status403Forbidden)
             .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet(string.Empty, CampaignsHandlers.GetCampaigns)
             .WithName(nameof(CampaignsHandlers.GetCampaigns))
             .WithSummary("Gets the list of all campaigns using the provided ListOptions.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGNS_DESCRIPTION);

        group.MapGet("{campaignId}", CampaignsHandlers.GetCampaignById)
             .WithName(nameof(CampaignsHandlers.GetCampaignById))
             .WithSummary("Gets a campaign with the specified id.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_BY_ID_DESCRIPTION);

        group.MapPut("{campaignId}/publish", CampaignsHandlers.PublishCampaign)
             .WithName(nameof(CampaignsHandlers.PublishCampaign))
             .WithSummary("Publishes a campaign.")
             .WithDescription(CampaignsHandlers.PUBLISH_CAMPAIGN_DESCRIPTION);

        group.MapGet("{campaignId}/statistics", CampaignsHandlers.GetCampaignStatistics)
             .WithName(nameof(CampaignsHandlers.GetCampaignStatistics))
             .WithSummary("Gets the statistics for a specified campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_STATISTICS_DESCRIPTION);

        group.MapPost(string.Empty, CampaignsHandlers.CreateCampaign)
             .WithName(nameof(CampaignsHandlers.CreateCampaign))
             .WithSummary("Creates a new campaign.")
             .WithDescription(CampaignsHandlers.CREATE_CAMPAIGN_DESCRIPTION)
             .WithParameterValidation<CreateCampaignRequest>()
             .WithExampleCreateCampaign();

        group.MapPut("{campaignId}", CampaignsHandlers.UpdateCampaign)
             .WithName(nameof(CampaignsHandlers.UpdateCampaign))
             .WithSummary("Updates an existing unpublished campaign.")
             .WithDescription(CampaignsHandlers.UPDATE_CAMPAIGN_DESCRIPTION)
             .WithParameterValidation<UpdateCampaignRequest>();

        group.MapDelete("{campaignId}", CampaignsHandlers.DeleteCampaign)
             .WithName(nameof(CampaignsHandlers.DeleteCampaign))
             .WithSummary("Permanently deletes a campaign.")
             .WithDescription(CampaignsHandlers.DELETE_CAMPAIGN_DESCRIPTION);

        group.MapPost("{campaignId}/attachment", CampaignsHandlers.UploadCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.UploadCampaignAttachment))
             .WithSummary("Uploads an attachment for the specified campaign.")
             .WithDescription(CampaignsHandlers.UPLOAD_CAMPAIGN_ATTACHMENT_DESCRIPTION)
             .WithParameterValidation<UploadFileRequest>()
             .Accepts<UploadFileRequest>("multipart/form-data")
             .LimitUpload(options.FileUploadLimit);

        group.MapDelete("{campaignId}/attachments/{attachmentId}", CampaignsHandlers.DeleteCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.DeleteCampaignAttachment))
             .WithSummary("Deletes the camapaign attachment")
             .WithDescription(CampaignsHandlers.DELETE_CAMPAIGN_ATTACHMENT_DESCRIPTION);

        group.MapGet("attachments/{fileGuid}.{format}", CampaignsHandlers.GetCampaignAttachment)
             .WithName(nameof(CampaignsHandlers.GetCampaignAttachment))
             .WithSummary("Gets the attachment associated with a campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_ATTACHMENT_DESCRIPTION)
             .AllowAnonymous()
             .ExcludeFromDescription();

        group.MapGet("{campaignId}/messages", CampaignsHandlers.GetCampaignMessages)
             .WithName(nameof(CampaignsHandlers.GetCampaignMessages))
             .WithSummary("Gets the messages send for this campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_MESSAGES);

        group.MapGet("{campaignId}/message/{messageId}", CampaignsHandlers.GetCampaignMessageDetails)
             .WithName(nameof(CampaignsHandlers.GetCampaignMessageDetails))
             .WithSummary("Gets information about the message of this campaign.")
             .WithDescription(CampaignsHandlers.GET_CAMPAIGN_MESSAGE_DETAILS);

        return group;
    }

    private static IEndpointConventionBuilder WithExampleCreateCampaign(this IEndpointConventionBuilder routeHandlerBuilder) =>
        routeHandlerBuilder.WithExampleRequestBody(new {
            TypeId = "type_alias",
            Title = $"Fancy campaign {DateTime.Now.Year}",
            ActivePeriod = new Period {
                From = DateTimeOffset.Now,
            },
            ActionLink = new Indice.Features.Messages.Core.Models.Hyperlink {
                Href = "https://www.indice.gr",
                Text = "click me"
            },
            IsGlobal = false,
            Published = true,
            RecipientIds = new string[] {
                    "known userId or customerCode 1",
                    "known userId or customerCode 2",
                    "known userId or customerCode 3"
                },
            RecipientListId = "list_alias",
            Recipients = new List<Indice.Features.Messages.Core.Models.ContactAnonymous> {
                    new () {
                        FirstName = "John",
                        LastName = "Doe",
                        FullName = "John Doe",
                        Email = "join-doe@example.com",
                        PhoneNumber = "+30 69XXXXXXXX"
                    },
                    new () {
                        FirstName = "Terrell",
                        LastName = "Levy",
                        FullName = "Terrell Levy",
                        Email = "terrelllevy@example.com",
                        PhoneNumber = "+1 (852) xxx-xxxx"
                    },
                },
            Data = new {
                firstField = "My parameter A",
                amount = 100.23,
                googleLogoSrc = "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png"
            },
            MessageTemplateId = "template_alias",
            Content = new Indice.Features.Messages.Core.Models.MessageContentDictionary() {
                ["Email"] = new Indice.Features.Messages.Core.Models.MessageContent() {
                    Title = "Hi {{contact.fullName}} 🎉!",
                    Body = @"<html><body>
    <h1>{{title}}<h1>
    <p>
        Hi {{contact.salutation}} {{contact.firstName}}.
        <br/>
        Check out this awsome offer here:
        <br/>
        <a href=""{{actionLink.href}}"">{{actionLink.text}}</a>
    </p>
    <p>
        Extra data can be also bound like this. {{data.firstField}}. And the google logo 
        <img src=""{{data.googleLogoSrc}}""
    </p>
</body></html>"
                },
                ["SMS"] = new Indice.Features.Messages.Core.Models.MessageContent() {
                    Title = "Hi {{contact.fullName}} 🎉!",
                    Body = @"{{title}} 🎉
Hi {{contact.salutation}} {{contact.firstName}}.
Check out this awsome offer here:

{{actionLink.text}}: {{actionLink.href}}
Extra data can be also bound like this. {{data.firstField}}."
                }
            }
        });
}