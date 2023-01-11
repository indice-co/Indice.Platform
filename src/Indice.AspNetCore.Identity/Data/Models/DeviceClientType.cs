namespace Indice.AspNetCore.Identity.Data.Models
{
    /// <summary>Describes the possible types of a user device.</summary>
    public enum DeviceClientType
    {
        /// <summary>A browser.</summary>
        /// <remarks>Can be both a desktop or mobile browser.</remarks>
        Browser,
        /// <summary>A native application (i.e. iOS or Android).</summary>
        Native
    }
}
