using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Messages.Core.Models;

/// <summary>
/// An intermediate object that captures a campaign Id and its distribution list Id.
/// </summary>
public class CampaignDLId
{
    /// <summary>
    /// The campaign Id.
    /// </summary>
    public Guid CampaignId { get; set; }
    /// <summary>
    /// The distribution list Id.
    /// </summary>
    public Guid? DistributionListId { get; set; }
}
