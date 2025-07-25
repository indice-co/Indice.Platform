using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core;

/// <summary>Options used to configure the Messages statistic feature.</summary>
public class CampaignStatisticOptions
{
    /// <summary>Feature flag that can switch on/off the statistics feature.</summary>
    public bool EnableStatics { get; set; } = true;
}
