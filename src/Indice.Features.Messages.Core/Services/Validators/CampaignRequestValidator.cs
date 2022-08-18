using FluentValidation;
using Indice.Configuration;
using Indice.Extensions;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Messages.Core.Services.Validators
{
    /// <summary>Contains validation logic for <see cref="CampaignRequestBase"/>.</summary>
    public class CampaignRequestValidator<TCampaignRequest> : AbstractValidator<TCampaignRequest> where TCampaignRequest : CampaignRequestBase
    {
        private readonly IMessageTypeService _messageTypeService;
        private readonly IDistributionListService _distributionListService;

        /// <summary>Creates a new instance of <see cref="CreateCampaignRequestValidator"/>.</summary>
        public CampaignRequestValidator(IServiceProvider serviceProvider) {
            _messageTypeService = serviceProvider.GetRequiredService<IMessageTypeService>();
            _distributionListService = serviceProvider.GetRequiredService<IDistributionListService>();
            RuleFor(campaign => campaign.Title)
                .NotEmpty()
                .WithMessage("Please provide a title for the campaign.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Campaign title cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.MessageChannelKind)
                .Must(channel => channel != MessageChannelKind.None)
                .WithMessage($"Please specify the campaign channel.");
            RuleFor(campaign => campaign.Content)
                .Must(HaveContentAccordingToChannelKind)
                .WithMessage("Please provide content for all defined channels of the campaign.")
                .When(campaign => campaign.Content.Count > 0)
                .Must(content => content.Count > 0)
                .WithMessage("Please provide content for the campaign.");
            RuleFor(campaign => campaign.RecipientListId)
                .Must(id => id is null)
                .When(campaign => campaign.IsGlobal) // DistributionListId property must not be provided when campaign is global.
                .WithMessage("Cannot provide a distribution list id since the campaign global.")
                .MustAsync(BeExistingDistributionListId)
                .When(campaign => campaign.RecipientListId is not null) // Check that DistributionListId is valid, when it is provided.
                .WithMessage("Specified distribution list id is not valid.");
            RuleFor(campaign => campaign.TypeId)
                .MustAsync(BeExistingTypeId)
                .When(campaign => campaign.TypeId.HasValue) // Check that TypeId is valid, when it is provided.
                .WithMessage("Specified type id is not valid.");
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
        }

        private bool HaveContentAccordingToChannelKind(CampaignRequestBase campaign, Dictionary<string, MessageContent> content) {
            var channels = campaign.MessageChannelKind.GetFlagValues().ToList();
            channels.Remove(MessageChannelKind.None);
            foreach (var channel in channels) {
                var pair = content.Where(x => x.Key.ToLower() == channel.ToString().ToLower()).FirstOrDefault();
                if (pair.Key is null) {
                    return false;
                }
            }
            return true;
        }

        private async Task<bool> BeExistingTypeId(Guid? id, CancellationToken cancellationToken) => await _messageTypeService.GetById(id.Value) is not null;

        private async Task<bool> BeExistingDistributionListId(Guid? id, CancellationToken cancellationToken) => await _distributionListService.GetById(id.Value) is not null;
    }
}
