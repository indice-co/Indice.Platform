using System;
using System.Threading.Tasks;
using Indice.AspNetCore.Features.Campaigns;
using Indice.AspNetCore.Features.Campaigns.Models;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Api.Controllers
{
    [Route("api/dummy")]
    public class DummyController : ControllerBase
    {
        public DummyController(CampaignManager campaignManager) {
            CampaignManager = campaignManager ?? throw new ArgumentNullException(nameof(campaignManager));
        }

        public CampaignManager CampaignManager { get; }

        [HttpPost]
        public async Task<IActionResult> CreateCampaign() {
            var campaign = new CreateCampaignRequest {
                Title = "Test",
                Content = "Test Content"
            };
            var result = await CampaignManager.CreateCampaign(campaign);
            return Ok(campaign);
        }
    }
}
