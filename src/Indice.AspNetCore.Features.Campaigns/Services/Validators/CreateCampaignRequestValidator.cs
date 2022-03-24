using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Indice.AspNetCore.Features.Campaigns.Data;
using Indice.AspNetCore.Features.Campaigns.Models;
using Indice.Configuration;
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
            DbContext = serviceProvider.GetRequiredService<CampaignsDbContext>();
            RuleFor(campaign => campaign.Title)
                .NotEmpty()
                .WithMessage("Please provide a title for the campaign.")
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Campaign title cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.SelectedUserCodes)
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
            RuleFor(campaign => campaign.ActionText)
                .MaximumLength(TextSizePresets.M128)
                .WithMessage($"Campaign action text cannot exceed {TextSizePresets.M128} characters.");
            RuleFor(campaign => campaign.ActionUrl)
                .MaximumLength(TextSizePresets.L2048)
                .WithMessage($"Campaign action URL cannot exceed {TextSizePresets.L2048} characters.")
                .Matches(@"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$")
                .WithMessage($"Campaign action URL is not valid.");
        }

        private CampaignsDbContext DbContext { get; }

        private async Task<bool> BeExistingTypeId(Guid? typeId, CancellationToken cancellationToken) => await DbContext.MessageTypes.FindAsync(typeId) is not null;

        private async Task<bool> BeExistingDistributionListId(Guid? typeId, CancellationToken cancellationToken) => await DbContext.DistributionLists.FindAsync(typeId) is not null;
    }
}
