namespace Indice.AspNetCore.Identity.Models
{
    /// <summary>
    /// Models application settings stored in the database.
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// Primary key for setting.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// The key of application setting.
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The value of application setting.
        /// </summary>
        public string Value { get; set; }
    }
}
