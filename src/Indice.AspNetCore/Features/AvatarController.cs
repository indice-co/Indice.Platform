using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Indice.Extensions;
using Indice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Indice.AspNetCore.Features
{
    /// <summary>
    /// Creates an avatar based on a given name (first and last name) plus parameters
    /// </summary>
    [Route("avatar")]
    internal class AvatarController : Controller 
    {
        /// <summary>
        /// avatar controller constructor
        /// </summary>
        public AvatarController() {

        }


        /// <summary>
        /// Creates an avatar using random background color based on fullname and optional size.
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        [HttpGet("{fullname}/{size?}"), ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetAvatar([FromRoute] string fullname, [FromRoute] int? size) => GetAvatar(fullname, size, null);
        
        /// <summary>
        /// Creates an avatar using random background color based on fullname and optional size and extension.
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="size"></param>
        /// <param name="ext">the file extension (png, jpg)</param>
        /// <returns></returns>
        [HttpGet("{fullname}/{size}.{ext?}"), ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetAvatar([FromRoute] string fullname, [FromRoute] int? size, [FromRoute] string ext) => GetAvatar(fullname, size, null, ext);


        /// <summary>
        /// Creates an avatar using fullname, size, background color and optional extension.
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="size"></param>
        /// <param name="background"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        [HttpGet("{fullname}/{size}/{background}.{ext?}"), ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "v" })]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetAvatar([FromRoute] string fullname, [FromRoute] int? size, [FromRoute] string background, [FromRoute] string ext) {
            if (string.IsNullOrWhiteSpace(fullname)) {
                return BadRequest();
            }

            var parts = fullname.Trim().Split(' ');
            var firstName = parts[0];
            var lastName = fullname.Trim().Remove(0, firstName.Length).TrimStart();

            if (parts.Length == 1) {
                parts = Regex.Split(firstName, @"(?<=\p{L})(?=\p{Lu}\p{Ll})|(?<=[\p{Ll}0-9])(?=[0-9]?\p{Lu})");
                firstName = parts[0];
                lastName = fullname.Trim().Remove(0, firstName.Length).TrimStart();
            }

            return GetAvatar(firstName, lastName, size, ext, background, null);
        }

        /// <summary>
        /// Creates an avatar based on first and last name plus parameters
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="size"></param>
        /// <param name="ext"></param>
        /// <param name="background"></param>
        /// <param name="v">cache buster</param>
        /// <returns></returns>
        [HttpGet("avatar"), ResponseCache(Duration = 345600, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "firstname", "lastname", "size", "ext", "background", "v" })]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public IActionResult GetAvatar([FromQuery] string firstName, [FromQuery] string lastName, [FromQuery] int? size, [FromQuery] string ext, [FromQuery] string background, [FromQuery] string v) {
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName)) {
                ModelState.AddModelError(nameof(firstName), "Must provide with first or last name");
                return BadRequest(ModelState);
            }

            if (size.HasValue && !new[] { 16, 24, 32, 48, 64, 128, 192, 256, 512 }.Contains((int)size)) {
                ModelState.AddModelError(nameof(size), "Size is out of range. Valid sizes are 16, 24, 32, 48, 64, 128, 192, 256, 512");
                return BadRequest(ModelState);
            }

            if (!string.IsNullOrEmpty(ext) && !new[] { "png", "jpg" }.Contains(ext)) {
                ModelState.AddModelError(nameof(size), "extension is out of range of valid values. Accepts only .png and .jpg");
                return BadRequest(ModelState);
            }

            var data = new MemoryStream();
            new AvatarGenerator().Generate(data, firstName, lastName, size ?? 192, ext == "jpg", background);
            var hash = string.Empty;

            using (var md5 = MD5.Create()) {
                hash = md5.ComputeHash(Encoding.UTF8.GetBytes($"{firstName} {lastName}")).ToBase64UrlSafe();
            }

            //var filename = $"{firstName}_{lastName}_{size ?? 192}.{ext}";
            return File(data, ext == "jpg" ? "image/jpeg" : "image/png", DateTime.UtcNow, new EntityTagHeaderValue($"\"{hash}\""));
        }
    }
}
