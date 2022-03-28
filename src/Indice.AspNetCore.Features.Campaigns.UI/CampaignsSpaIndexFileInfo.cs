using System;
using System.Collections.Generic;
using Indice.AspNetCore.EmbeddedUI;
using Microsoft.Extensions.FileProviders;

namespace Indice.AspNetCore.Features.Campaigns.UI
{
    /// <summary>
    /// Represents the starting point file for a SPA (index.html) in the given file provider.
    /// </summary>
    public class CampaignsSpaIndexFileInfo : SpaIndexFileInfo<CampaignUIOptions>
    {
        private readonly CampaignUIOptions _options;

        /// <summary>
        /// Creates a new instance of <see cref="CampaignsSpaIndexFileInfo"/>.
        /// </summary>
        /// <param name="fileInfo">The type of <see cref="IFileInfo"/> provider.</param>
        /// <param name="options">Options for configuring <see cref="SpaUIMiddleware{TOptions}"/> middleware.</param>
        public CampaignsSpaIndexFileInfo(IFileInfo fileInfo, CampaignUIOptions options) : base(fileInfo, options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc />
        protected override IDictionary<string, string> GetIndexArguments() {
            var arguments = base.GetIndexArguments();
            arguments.Add("%(ActiveDeliveryChannels)", _options.ActiveDeliveryChannels.ToString());
            return arguments;
        }
    }
}
