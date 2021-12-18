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
                .Must(userCodes => userCodes.Count > 0)
                .When(campaign => !campaign.IsGlobal)
                .WithMessage("Please provide a list of recipients since the campaign is not intended for all users (global).");
            RuleFor(campaign => campaign.TypeId)
                .MustAsync(BeExistingTypeId)
                .When(campaign => campaign.TypeId is not null);
            RuleFor(campaign => campaign.Content)
                .NotEmpty()
                .WithMessage("Please provide content for the campaign.");
            RuleFor(campaign => campaign.ActivePeriod.From)
                .Must(from => from.Value >= DateTimeOffset.UtcNow)
                .When(campaign => campaign.ActivePeriod?.From is not null)
                .WithMessage("Campaign should start now or in a future date.");
            RuleFor(campaign => campaign.ActivePeriod)
                .Must(campaign => campaign.To > campaign.From)
                .When(campaign => campaign.ActivePeriod?.From is not null && campaign.ActivePeriod?.To is not null)
                .WithMessage("Campaign should end after the start date.");
        }

        private CampaignsDbContext DbContext { get; }

        private async Task<bool> BeExistingTypeId(Guid? typeId, CancellationToken cancellationToken) => await DbContext.CampaignTypes.FindAsync(typeId, cancellationToken) is not null;
    }
}
