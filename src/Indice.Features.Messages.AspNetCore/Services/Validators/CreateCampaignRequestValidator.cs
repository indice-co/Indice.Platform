using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Indice.Configuration;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Features.Campaigns.Services
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
            RuleFor(campaign => campaign.Title)
                .NotEmpty()
                .WithMessage("Please provide a title for the campaign.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Campaign title cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.SelectedRecipientIds)
                .Must(userCodes => userCodes?.Count == 0)
                .When(campaign => campaign.IsGlobal)
                .WithMessage("Cannot provide a list of recipients since the campaign global.");
            RuleFor(campaign => campaign.TypeId)
                .MustAsync(BeExistingTypeId)
                .When(campaign => campaign.TypeId is not null)
                .WithMessage("Specified type id is not valid.");
            RuleFor(campaign => campaign.DistributionListId)
                .Must(id => id == null)
                .When(campaign => campaign.IsGlobal)
                .WithMessage("Cannot provide a distribution list since the campaign global.")
                .MustAsync(BeExistingDistributionListId)
                .When(campaign => campaign.DistributionListId is not null)
                .WithMessage("Specified distribution list id is not valid.");
            RuleFor(campaign => campaign.Content)
                .NotEmpty()
                .WithMessage("Please provide content for the campaign.");
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
                .WithMessage($"Campaign action text cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.ActionLink.Href)
                .MaximumLength(TextSizePresets.L1024)
                .WithMessage($"Campaign action URL cannot exceed {TextSizePresets.L1024} characters.")
                .Matches(@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$")
                .WithMessage($"Campaign action URL is not valid.");
        }

        private IMessageTypeService MessageTypeService { get; }
        private IDistributionListService DistributionListService { get; }

        private async Task<bool> BeExistingTypeId(Guid? id, CancellationToken cancellationToken) => await MessageTypeService.GetById(id.Value) is not null;

        private async Task<bool> BeExistingDistributionListId(Guid? id, CancellationToken cancellationToken) => await DistributionListService.GetById(id.Value) is not null;
    }
}
