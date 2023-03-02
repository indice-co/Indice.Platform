using System.Net.Mime;
using Indice.Api.JobHandlers;
using Indice.Hosting.Services;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Api.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "workers")]
    [Consumes(MediaTypeNames.Application.Json)]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(statusCode: 401, type: typeof(ProblemDetails))]
    [ProducesResponseType(statusCode: 403, type: typeof(ProblemDetails))]
    [Route("api")]
    public class WebsitesController : ControllerBase
    {
        private readonly IMessageQueue<ExtractWebsitesCommand> _messageQueue;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public WebsitesController(
            IMessageQueue<ExtractWebsitesCommand> messageQueue,
            IWebHostEnvironment webHostEnvironment
        ) {
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
        }

        [HttpGet("download-top-websites")]
        [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DownloadTop([FromQuery(Name = "l")] int? limit) {
            var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "JobHandlers", "top1000websites.csv");
            await _messageQueue.Enqueue(new ExtractWebsitesCommand(filePath, limit));
            return NoContent();
        }
    }
}
