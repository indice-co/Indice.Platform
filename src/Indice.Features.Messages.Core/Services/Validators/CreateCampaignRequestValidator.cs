using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>
    /// Contains validation logic for <see cref="CreateCampaignRequest"/>.
    /// </summary>
    public class CreateCampaignRequestValidator : AbstractValidator<CreateCampaignRequest>
    {
        /// <summary>
        /// Creates a new instance of <see cref="CreateCampaignRequestValidator"/>.
        /// </summary>
        public CreateCampaignRequestValidator(IServiceProvider serviceProvider) {
            MessageTypeService = serviceProvider.GetRequiredService<IMessageTypeService>();
            DistributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            TemplateService = serviceProvider.GetRequiredService<ITemplateService>();
            RuleFor(campaign => campaign.Title)
                .NotEmpty()
                .WithMessage("Please provide a title for the campaign.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Campaign title cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.MessageChannelKind)
                .Must(channel => channel != MessageChannelKind.None)
                .WithMessage($"Please specify the campaign channel.");
            RuleFor(campaign => campaign.Content)
                .NotEmpty()
                .WithMessage("Please provide content for the campaign.")
                .When(x => !x.TemplateId.HasValue && x.Published); // Content property must not be empty when the campaign is Published and a TemplateId is not provided at the same time.
            RuleFor(campaign => campaign.RecipientIds)
                .Must(recipientIds => recipientIds?.Count == 0)
                .When(campaign => campaign.IsGlobal) // RecipientIds property must be empty when campaign is global.
                .WithMessage("Cannot provide a list of recipients since the campaign global.")
                .Must(recipientIds => recipientIds?.Count > 0)
                .When(campaign => !campaign.DistributionListId.HasValue && !campaign.IsGlobal) // RecipientIds property must not be empty when a DistributionListId is not provided.
                .WithMessage("Please provide either recipient ids or a distribution list id.");
            RuleFor(campaign => campaign.TypeId)
                .MustAsync(BeExistingTypeId)
                .When(campaign => campaign.TypeId.HasValue) // Check that TypeId is valid, when it is provided.
                .WithMessage("Specified type id is not valid.");
            RuleFor(campaign => campaign.DistributionListId)
                .Must(id => id is null)
                .When(campaign => campaign.IsGlobal) // DistributionListId property must not be provided when campaign is global.
                .WithMessage("Cannot provide a distribution list id since the campaign global.")
                .MustAsync(BeExistingDistributionListId)
                .When(campaign => campaign.DistributionListId is not null) // Check that DistributionListId is valid, when it is provided.
                .WithMessage("Specified distribution list id is not valid.")
                .Must(id => id is not null)
                .When(campaign => campaign.RecipientIds?.Count == 0 && !campaign.IsGlobal)
                .WithMessage("Please provide either recipient ids or a distribution list id.");
            RuleFor(campaign => campaign.ActivePeriod.From)
                .Must(from => from.Value.Date >= DateTimeOffset.UtcNow.Date)
                .When(campaign => campaign.ActivePeriod?.From is not null)
                .WithMessage("Campaign should start now or in a future date.");
            RuleFor(campaign => campaign.ActivePeriod)
                .Must(campaign => campaign.To > campaign.From)
                .When(campaign => campaign.ActivePeriod?.From is not null && campaign.ActivePeriod?.To is not null)
                .WithMessage("Campaign should end after the start date.");
            RuleFor(campaign => campaign.ActionLink.Text)
                .MaximumLength(TextSizePresets.M128)
                .When(x => !string.IsNullOrWhiteSpace(x.ActionLink?.Text))
                .WithMessage($"Campaign action text cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.ActionLink.Href)
                .MaximumLength(TextSizePresets.L1024)
                .WithMessage($"Campaign action URL cannot exceed {TextSizePresets.L1024} characters.")
                .Matches(@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$")
                .WithMessage($"Campaign action URL is not valid.")
                .When(x => !string.IsNullOrWhiteSpace(x.ActionLink?.Href));
            RuleFor(campaign => campaign.TemplateId)
                .MustAsync(BeExistingTemplateId)
                .When(campaign => campaign.TemplateId.HasValue) // Check that TemplateId is valid, when it is provided.
                .WithMessage("Specified template id is not valid.")
                .Must(id => id is null)
                .When(campaign => campaign.Content?.Count > 0)
                .WithMessage("Cannot provide a template id when submitting campaign content.");
        }

        private IMessageTypeService MessageTypeService { get; }
        private IDistributionListService DistributionListService { get; }
        private ITemplateService TemplateService { get; }

        private async Task<bool> BeExistingTypeId(Guid? id, CancellationToken cancellationToken) => await MessageTypeService.GetById(id.Value) is not null;

        private async Task<bool> BeExistingDistributionListId(Guid? id, CancellationToken cancellationToken) => await DistributionListService.GetById(id.Value) is not null;

        private async Task<bool> BeExistingTemplateId(Guid? id, CancellationToken cancellationToken) => await TemplateService.GetById(id.Value) is not null;
    }
}
