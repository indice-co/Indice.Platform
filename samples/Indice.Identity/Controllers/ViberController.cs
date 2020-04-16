using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Indice.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using QRCoder;

namespace Indice.Identity.Controllers
{
    [ApiController]
    [Route("api/viber")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ViberController : ControllerBase
    {
        private const string PngMimeType = "image/png";

        /// <summary>
        /// An endpoint (webhook) that captures the configured event types coming from Viber.
        /// </summary>
        /// <response code="200">OK</response>
        [HttpPost("webhook")]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        public IActionResult Webhook([FromBody]string content) {
            return Ok();
        }

        /// <summary>
        /// Returns a QR code image containg a deep link direct to info page of the configured Viber bot.
        /// </summary>
        /// <param name="size">The size in pixels of the QR code image. Available sizes are 64, 128, 256, 512, 1024.</param>
        /// <param name="context">The value of this parameter will be forwarded to the bot under the context parameter in the conversation started callback.</param>
        /// <param name="text">The value of this parameter will appear in the text input field when the 1-on-1 chat with the bot is opened by pressing the deep link.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpGet("link-info/qr-code")]
        [Produces(MediaTypeNames.Application.Octet, new[] { MediaTypeNames.Application.Json })]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "size", "context", "text", "v" })]
        public IActionResult GetLinkInfoQrCode([FromQuery]int? size, [FromQuery]string context, [FromQuery]string text) =>
            GetQrCode(size, "viber://pa/info?uri=gmanoltzas", context, text);

        /// <summary>
        /// Returns a QR code image containg a deep link direct to 1on1 chat with the configured Viber bot.
        /// </summary>
        /// <param name="size">The size in pixels of the QR code image. Available sizes are 64, 128, 256, 512, 1024.</param>
        /// <param name="context">The value of this parameter will be forwarded to the bot under the context parameter in the conversation started callback.</param>
        /// <param name="text">The value of this parameter will appear in the text input field when the 1-on-1 chat with the bot is opened by pressing the deep link.</param>
        /// <response code="200">OK</response>
        /// <response code="400">Bad Request</response>
        [HttpGet("link-direct/qr-code")]
        [Produces(MediaTypeNames.Application.Octet, new[] { MediaTypeNames.Application.Json })]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IFormFile))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
        [ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "size", "context", "text", "v" })]
        public IActionResult GetLinkDirectQrCode([FromQuery]int? size, [FromQuery]string context, [FromQuery]string text) =>
            GetQrCode(size, "viber://pa?chatURI=gmanoltzas", context, text);

        private IActionResult GetQrCode(int? size, string deepLinkBase, string context, string text) {
            var availableSizes = new int[] { 64, 128, 256, 512, 1024 };
            if (size.HasValue && !availableSizes.Contains(size.Value)) {
                ModelState.AddModelError(nameof(size), "Please select one of the following sizes: 64, 128, 256, 512 and 1024.");
                return BadRequest();
            }
            size ??= availableSizes[2];
            var deepLink = new StringBuilder(deepLinkBase);
            if (!string.IsNullOrEmpty(context)) {
                deepLink.Append($"?context={context}");
            }
            if (!string.IsNullOrEmpty(text)) {
                deepLink.Append($"{(!string.IsNullOrEmpty(context) ? "&" : "?")}text={text}");
            }
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(deepLink.ToString(), QRCodeGenerator.ECCLevel.H);
            var qrCode = new QRCode(qrCodeData);
            var qrCodeBitmap = qrCode.GetGraphic(pixelsPerModule: 30);
            var resizedQrCodeBitmap = ScaleImage(qrCodeBitmap, size.Value);
            var hash = string.Empty;
            using (var md5 = MD5.Create()) {
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{context}{text}")).ToBase64UrlSafe();
            }
            using var memoryStream = new MemoryStream();
            resizedQrCodeBitmap.Save(memoryStream, ImageFormat.Png);
            return File(memoryStream.ToArray(), PngMimeType, DateTime.UtcNow, new EntityTagHeaderValue($"\"{hash}\""));
        }

        private static Image ScaleImage(Image image, int height) {
            var ratio = (double)height / image.Height;
            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);
            var newImage = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(newImage)) {
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            image.Dispose();
            return newImage;
        }
    }
}
