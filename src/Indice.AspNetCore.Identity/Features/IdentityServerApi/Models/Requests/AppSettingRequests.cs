namespace Indice.AspNetCore.Identity.Api.Models
{
    /// <summary>
    /// Models an application setting persisted in the database.
    /// </summary>
    public class CreateAppSettingRequest
    {
        /// <summary>
        /// The key of application setting.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The value of application setting.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Models an application setting that will be updated on the server.
    /// </summary>
    public class UpdateAppSettingRequest
    {
        /// <summary>
        /// The value of application setting.
        /// </summary>
        public string Value { get; set; }
    }
}
